using MarketService.Api.Hubs;
using MarketService.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace MarketService.Api.Realtime;

internal sealed class SignalRTradingPetsRealtimePublisher : ITradingPetsRealtimePublisher
{
    private readonly IHubContext<MarketHub> _hubContext;

    public SignalRTradingPetsRealtimePublisher(IHubContext<MarketHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyTraderSnapshotRefreshAsync(Guid traderId, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(MarketHub.TradingPetsTraderGroup(traderId))
            .SendAsync("trader.snapshotUpdated", new { traderId, reason = "refresh" }, cancellationToken);
    }

    public Task NotifyMarketListingsRefreshAsync(CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(MarketHub.TradingPetsMarketGroup)
            .SendAsync("market.listingsUpdated", new { reason = "listingChanged" }, cancellationToken);
    }

    public Task NotifyTraderNotificationsAsync(Guid traderId, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(MarketHub.TradingPetsTraderGroup(traderId))
            .SendAsync("trader.notificationsAdded", new { traderId, reason = "refresh" }, cancellationToken);
    }

    public Task NotifyLeaderboardRefreshAsync(CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(MarketHub.TradingPetsLeaderboardGroup)
            .SendAsync("leaderboard.updated", new { reason = "refresh" }, cancellationToken);
    }

    public Task NotifyPetValuationBatchAsync(
        IReadOnlyList<Guid> petIds,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(MarketHub.TradingPetsMarketGroup)
            .SendAsync(
                "pet.valuationBatch",
                new { petIds, effectiveAt = DateTimeOffset.UtcNow },
                cancellationToken);
    }
}
