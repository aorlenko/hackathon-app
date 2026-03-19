using MarketService.Application.Abstractions;
using Trading.Contracts.Events;
using Trading.Messaging;

namespace MarketService.Infrastructure.Messaging;

public sealed class LifecycleEventPublisher : ILifecycleEventPublisher
{
    private readonly IEventBus _eventBus;

    public LifecycleEventPublisher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default) where TEvent : class, ITradingEvent
    {
        return _eventBus.PublishAsync(message, cancellationToken);
    }
}
