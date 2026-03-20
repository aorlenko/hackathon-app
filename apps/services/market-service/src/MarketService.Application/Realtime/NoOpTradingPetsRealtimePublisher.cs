namespace MarketService.Application.Realtime;

public sealed class NoOpTradingPetsRealtimePublisher : ITradingPetsRealtimePublisher
{
    public Task NotifyTraderSnapshotRefreshAsync(Guid traderId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyMarketListingsRefreshAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyTraderNotificationsAsync(Guid traderId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyLeaderboardRefreshAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyPetValuationBatchAsync(
        IReadOnlyList<Guid> petIds,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
