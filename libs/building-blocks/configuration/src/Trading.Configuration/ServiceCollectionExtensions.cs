using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Trading.Configuration;

public sealed class TradingPlatformOptions
{
    public string ServiceName { get; set; } = "trading-service";
    public MatchingOptions Matching { get; set; } = new();
    public SettlementOptions Settlement { get; set; } = new();
    public MessagingOptions Messaging { get; set; } = new();
}

public sealed class MatchingOptions
{
    public decimal MinimumPrice { get; set; } = 1m;
    public decimal MaximumPrice { get; set; } = 100000m;
}

public sealed class SettlementOptions
{
    public bool SimulateFailure { get; set; }
    public int DelayMilliseconds { get; set; } = 1;
}

public enum MessagingTransport
{
    AzureServiceBus,
    InMemory
}

public sealed class MessagingOptions
{
    public MessagingTransport Transport { get; set; } = MessagingTransport.AzureServiceBus;
    public string TopicName { get; set; } = "trading.lifecycle";
    public string SubscriptionName { get; set; } = string.Empty;
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTradingPlatformConfiguration(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        services.AddOptions<TradingPlatformOptions>()
            .Bind(configuration.GetSection("TradingPlatform"))
            .PostConfigure(options =>
            {
                configuration.GetSection("Messaging").Bind(options.Messaging);

                if (string.IsNullOrWhiteSpace(options.ServiceName))
                {
                    options.ServiceName = serviceName;
                }
            });

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TradingPlatformOptions>>().Value);
        return services;
    }
}
