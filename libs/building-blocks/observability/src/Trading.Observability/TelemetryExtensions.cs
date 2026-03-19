using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trading.Observability;

public static class TelemetryExtensions
{
    public static IServiceCollection AddTradingTelemetry(this IServiceCollection services, string serviceName)
    {
        services.AddLogging();
        services.AddMetrics();
        services.AddHealthChecks().AddCheck(serviceName, () => HealthCheckResult.Healthy());
        services.AddSingleton<LifecycleLatencyMetrics>();
        return services;
    }
}
