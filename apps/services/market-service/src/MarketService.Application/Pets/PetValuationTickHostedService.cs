using MarketService.Application.Abstractions;
using MarketService.Application.Realtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketService.Application.Pets;

public sealed class PetValuationTickHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PetValuationTickHostedService> _logger;
    private readonly TradingPetsOptions _options;

    public PetValuationTickHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<TradingPetsOptions> options,
        ILogger<PetValuationTickHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.ValuationTickSeconds));
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<IMarketPetStore>();
                    var notifier = scope.ServiceProvider.GetRequiredService<ITradingPetsRealtimePublisher>();
                    var petIds = await store.RunValuationTickAsync(stoppingToken).ConfigureAwait(false);
                    await notifier.NotifyPetValuationBatchAsync(petIds, stoppingToken).ConfigureAwait(false);
                    await notifier.NotifyMarketListingsRefreshAsync(stoppingToken).ConfigureAwait(false);
                    await notifier.NotifyLeaderboardRefreshAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Trading pets valuation tick failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }
}
