using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trading.Configuration;
using Trading.Contracts.Events;

namespace Trading.Messaging;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default) where TEvent : class, ITradingEvent;
}

public interface IIntegrationEventHandler<in TEvent> where TEvent : class, ITradingEvent
{
    Task HandleAsync(TEvent message, CancellationToken cancellationToken = default);
}

public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Func<ITradingEvent, CancellationToken, Task>>> _subscriptions = new();

    public async Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default) where TEvent : class, ITradingEvent
    {
        if (!_subscriptions.TryGetValue(typeof(TEvent), out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.ToArray())
        {
            await handler(message, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class, ITradingEvent
    {
        Subscribe(typeof(TEvent), (message, cancellationToken) => handler((TEvent)message, cancellationToken));
    }

    internal void Subscribe(Type eventType, Func<ITradingEvent, CancellationToken, Task> handler)
    {
        var handlers = _subscriptions.GetOrAdd(eventType, _ => []);
        handlers.Add(handler);
    }
}

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddTradingMessaging(this IServiceCollection services)
    {
        services.TryAddSingleton(_ => new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        });
        services.TryAddSingleton<InMemoryEventBus>();
        services.TryAddSingleton<AzureServiceBusEventBus>();
        services.TryAddSingleton<IEventBus>(sp =>
        {
            var options = sp.GetRequiredService<TradingPlatformOptions>().Messaging;
            return options.Transport switch
            {
                MessagingTransport.InMemory => sp.GetRequiredService<InMemoryEventBus>(),
                _ => sp.GetRequiredService<AzureServiceBusEventBus>()
            };
        });
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MessagingHostedService>());

        return services;
    }

    public static IServiceCollection AddIntegrationEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : class, ITradingEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        services.TryAddScoped<THandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IIntegrationEventHandlerRegistration, IntegrationEventHandlerRegistration<TEvent, THandler>>());
        return services;
    }
}

internal interface IIntegrationEventHandlerRegistration
{
    string EventType { get; }
    Type EventClrType { get; }
    Task HandleAsync(IServiceProvider services, ITradingEvent message, CancellationToken cancellationToken);
}

internal sealed class IntegrationEventHandlerRegistration<TEvent, THandler> : IIntegrationEventHandlerRegistration
    where TEvent : class, ITradingEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    public string EventType => typeof(TEvent).Name;

    public Type EventClrType => typeof(TEvent);

    public Task HandleAsync(IServiceProvider services, ITradingEvent message, CancellationToken cancellationToken)
    {
        return services.GetRequiredService<THandler>().HandleAsync((TEvent)message, cancellationToken);
    }
}

public sealed class AzureServiceBusEventBus : IEventBus, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly JsonSerializerOptions _serializerOptions;

    public AzureServiceBusEventBus(IConfiguration configuration, TradingPlatformOptions options, JsonSerializerOptions serializerOptions)
    {
        _serializerOptions = serializerOptions;
        var connectionString = ResolveConnectionString(configuration);
        var topicName = ResolveTopicName(options);
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topicName);
    }

    public Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default) where TEvent : class, ITradingEvent
    {
        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromString(JsonSerializer.Serialize(message, _serializerOptions)))
        {
            ContentType = "application/json",
            CorrelationId = message.CorrelationId,
            MessageId = message.EventId.ToString("N"),
            Subject = message.EventType
        };

        serviceBusMessage.ApplicationProperties["eventType"] = message.EventType;
        serviceBusMessage.ApplicationProperties["eventVersion"] = message.EventVersion;
        serviceBusMessage.ApplicationProperties["producer"] = message.Producer;
        serviceBusMessage.ApplicationProperties["correlationId"] = message.CorrelationId;

        return _sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync().ConfigureAwait(false);
        await _client.DisposeAsync().ConfigureAwait(false);
    }

    internal static string ResolveConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("ServiceBus")
            ?? throw new InvalidOperationException("ConnectionStrings:ServiceBus must be configured when Messaging:Transport is AzureServiceBus.");
    }

    internal static string ResolveTopicName(TradingPlatformOptions options)
    {
        return string.IsNullOrWhiteSpace(options.Messaging.TopicName)
            ? throw new InvalidOperationException("Messaging:TopicName must be configured.")
            : options.Messaging.TopicName;
    }
}

internal sealed class InMemoryEventSubscriptionHostedService : IHostedService
{
    private readonly InMemoryEventBus _eventBus;
    private readonly IReadOnlyList<IIntegrationEventHandlerRegistration> _registrations;
    private readonly IServiceScopeFactory _scopeFactory;

    public InMemoryEventSubscriptionHostedService(
        InMemoryEventBus eventBus,
        IEnumerable<IIntegrationEventHandlerRegistration> registrations,
        IServiceScopeFactory scopeFactory)
    {
        _eventBus = eventBus;
        _registrations = registrations.ToList();
        _scopeFactory = scopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var registration in _registrations)
        {
            _eventBus.Subscribe(registration.EventClrType, async (message, token) =>
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await registration.HandleAsync(scope.ServiceProvider, message, token).ConfigureAwait(false);
            });
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class MessagingHostedService : IHostedService, IAsyncDisposable
{
    private readonly IHostedService _inner;

    public MessagingHostedService(IServiceProvider services, TradingPlatformOptions options)
    {
        _inner = options.Messaging.Transport switch
        {
            MessagingTransport.InMemory => ActivatorUtilities.CreateInstance<InMemoryEventSubscriptionHostedService>(services),
            _ => ActivatorUtilities.CreateInstance<AzureServiceBusSubscriptionWorker>(services)
        };
    }

    public Task StartAsync(CancellationToken cancellationToken) => _inner.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => _inner.StopAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        switch (_inner)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }
}

internal sealed class AzureServiceBusSubscriptionWorker : BackgroundService, IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly TradingPlatformOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger<AzureServiceBusSubscriptionWorker> _logger;
    private readonly Dictionary<string, List<IIntegrationEventHandlerRegistration>> _registrations;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    public AzureServiceBusSubscriptionWorker(
        IConfiguration configuration,
        TradingPlatformOptions options,
        IEnumerable<IIntegrationEventHandlerRegistration> registrations,
        IServiceScopeFactory scopeFactory,
        JsonSerializerOptions serializerOptions,
        ILogger<AzureServiceBusSubscriptionWorker> logger)
    {
        _configuration = configuration;
        _options = options;
        _scopeFactory = scopeFactory;
        _serializerOptions = serializerOptions;
        _logger = logger;
        _registrations = registrations
            .GroupBy(x => x.EventType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_registrations.Count == 0)
        {
            _logger.LogInformation("No integration event handlers registered for {ServiceName}.", _options.ServiceName);
            return;
        }

        var subscriptionName = string.IsNullOrWhiteSpace(_options.Messaging.SubscriptionName)
            ? throw new InvalidOperationException("Messaging:SubscriptionName must be configured for AzureServiceBus subscribers.")
            : _options.Messaging.SubscriptionName;

        _client = new ServiceBusClient(AzureServiceBusEventBus.ResolveConnectionString(_configuration));
        _processor = _client.CreateProcessor(
            AzureServiceBusEventBus.ResolveTopicName(_options),
            subscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false
            });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken).ConfigureAwait(false);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (_processor is not null)
            {
                await _processor.StopProcessingAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var eventType = ResolveEventType(args.Message);
        if (eventType is null || !_registrations.TryGetValue(eventType, out var registrations))
        {
            _logger.LogDebug("Ignoring message {MessageId} because no handler is registered for event type {EventType}.", args.Message.MessageId, eventType ?? "<missing>");
            await args.CompleteMessageAsync(args.Message, args.CancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            var message = Deserialize(args.Message, registrations[0].EventClrType);

            await using var scope = _scopeFactory.CreateAsyncScope();
            foreach (var registration in registrations)
            {
                await registration.HandleAsync(scope.ServiceProvider, message, args.CancellationToken).ConfigureAwait(false);
            }

            await args.CompleteMessageAsync(args.Message, args.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed processing message {MessageId} on subscription {SubscriptionName}.", args.Message.MessageId, _options.Messaging.SubscriptionName);
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken).ConfigureAwait(false);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Azure Service Bus processor error for {EntityPath}.", args.EntityPath);
        return Task.CompletedTask;
    }

    private string? ResolveEventType(ServiceBusReceivedMessage message)
    {
        if (message.ApplicationProperties.TryGetValue("eventType", out var eventType) && eventType is not null)
        {
            return eventType.ToString();
        }

        using var document = JsonDocument.Parse(message.Body.ToString());
        return document.RootElement.TryGetProperty("eventType", out var eventTypeProperty)
            ? eventTypeProperty.GetString()
            : null;
    }

    private ITradingEvent Deserialize(ServiceBusReceivedMessage message, Type eventClrType)
    {
        return JsonSerializer.Deserialize(message.Body.ToString(), eventClrType, _serializerOptions) as ITradingEvent
            ?? throw new InvalidOperationException($"Could not deserialize message {message.MessageId} to {eventClrType.Name}.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor is not null)
        {
            await _processor.DisposeAsync().ConfigureAwait(false);
        }

        if (_client is not null)
        {
            await _client.DisposeAsync().ConfigureAwait(false);
        }
    }
}
