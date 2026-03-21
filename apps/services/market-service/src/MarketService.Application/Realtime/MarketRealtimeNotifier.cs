using Trading.Contracts.Events;
using Trading.Contracts.Http;

namespace MarketService.Application.Realtime;

public interface IMarketHubPublisher
{
    Task PublishOrderBookAsync(string symbol, OrderBookDto payload, CancellationToken cancellationToken = default);
    Task PublishTradeAsync(string symbol, TradeRecordedRealtimeDto payload, CancellationToken cancellationToken = default);
    Task PublishSettlementAsync(IEnumerable<string> userIds, SettlementUpdatedRealtimeDto payload, CancellationToken cancellationToken = default);
    Task PublishFundsUpdatedAsync(string userId, FundsUpdatedRealtimeDto payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Legacy equity/settlement realtime paths. Trading pets uses <see cref="ITradingPetsRealtimePublisher"/> + SignalR groups on the same hub.
/// </summary>
public interface IMarketRealtimeNotifier
{
    Task NotifyOrderBookUpdatedAsync(string symbol, CancellationToken cancellationToken = default);
    Task NotifyTradeRecordedAsync(TradeRecorded @event, CancellationToken cancellationToken = default);
    Task NotifySettlementUpdatedAsync(string buyerUserId, string sellerUserId, SettlementUpdatedRealtimeDto payload, CancellationToken cancellationToken = default);
    Task NotifyFundsUpdatedAsync(FundsUpdatedRealtimeDto payload, CancellationToken cancellationToken = default);
    Task NotifyTradingPetsStateChangedAsync(
        IReadOnlyCollection<Guid> traderIds,
        bool includeNotifications = false,
        bool includeLeaderboard = false,
        CancellationToken cancellationToken = default);
}

public sealed class MarketRealtimeNotifier : IMarketRealtimeNotifier
{
    private readonly Markets.GetOrderBookHandler _orderBookHandler;
    private readonly IMarketHubPublisher _publisher;
    private readonly ITradingPetsRealtimePublisher? _tradingPetsPublisher;

    public MarketRealtimeNotifier(
        Markets.GetOrderBookHandler orderBookHandler,
        IMarketHubPublisher publisher,
        ITradingPetsRealtimePublisher? tradingPetsPublisher = null)
    {
        _orderBookHandler = orderBookHandler;
        _publisher = publisher;
        _tradingPetsPublisher = tradingPetsPublisher;
    }

    public async Task NotifyOrderBookUpdatedAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var snapshot = await _orderBookHandler.HandleAsync(symbol, cancellationToken).ConfigureAwait(false);
        await _publisher.PublishOrderBookAsync(symbol, snapshot, cancellationToken).ConfigureAwait(false);
    }

    public Task NotifyTradeRecordedAsync(TradeRecorded @event, CancellationToken cancellationToken = default)
    {
        var payload = new TradeRecordedRealtimeDto(
            @event.TradeId,
            @event.Symbol,
            @event.Price,
            @event.Quantity,
            @event.ExecutedAtUtc,
            @event.BuyerUserId,
            @event.SellerUserId);
        return _publisher.PublishTradeAsync(@event.Symbol, payload, cancellationToken);
    }

    public Task NotifySettlementUpdatedAsync(string buyerUserId, string sellerUserId, SettlementUpdatedRealtimeDto payload, CancellationToken cancellationToken = default)
    {
        return _publisher.PublishSettlementAsync([buyerUserId, sellerUserId], payload, cancellationToken);
    }

    public Task NotifyFundsUpdatedAsync(FundsUpdatedRealtimeDto payload, CancellationToken cancellationToken = default)
    {
        return _publisher.PublishFundsUpdatedAsync(payload.UserId, payload, cancellationToken);
    }

    public async Task NotifyTradingPetsStateChangedAsync(
        IReadOnlyCollection<Guid> traderIds,
        bool includeNotifications = false,
        bool includeLeaderboard = false,
        CancellationToken cancellationToken = default)
    {
        if (_tradingPetsPublisher is null)
        {
            return;
        }

        foreach (var traderId in traderIds.Distinct())
        {
            await _tradingPetsPublisher.NotifyTraderSnapshotRefreshAsync(traderId, cancellationToken)
                .ConfigureAwait(false);

            if (includeNotifications)
            {
                await _tradingPetsPublisher.NotifyTraderNotificationsAsync(traderId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        await _tradingPetsPublisher.NotifyMarketListingsRefreshAsync(cancellationToken).ConfigureAwait(false);

        if (includeLeaderboard)
        {
            await _tradingPetsPublisher.NotifyLeaderboardRefreshAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
