namespace MarketService.Application.Realtime;

public interface ITradingPetsRealtimePublisher
{
    Task NotifyTraderSnapshotRefreshAsync(Guid traderId, CancellationToken cancellationToken = default);

    Task NotifyMarketListingsRefreshAsync(CancellationToken cancellationToken = default);

    Task NotifyTraderNotificationsAsync(Guid traderId, CancellationToken cancellationToken = default);

    Task NotifyLeaderboardRefreshAsync(CancellationToken cancellationToken = default);

    Task NotifyPetValuationBatchAsync(
        IReadOnlyList<Guid> petIds,
        CancellationToken cancellationToken = default);
}
