using MarketService.Application.Realtime;
using Trading.Contracts.Http;

namespace Trading.TestSupport.Realtime;

public sealed class CollectingMarketHubPublisher : IMarketHubPublisher
{
    public List<OrderBookDto> OrderBooks { get; } = [];
    public List<TradeRecordedRealtimeDto> Trades { get; } = [];
    public List<(string UserId, FundsUpdatedRealtimeDto Payload)> FundsUpdates { get; } = [];
    public List<(string UserId, SettlementUpdatedRealtimeDto Payload)> Settlements { get; } = [];

    public Task PublishOrderBookAsync(string symbol, OrderBookDto payload, CancellationToken cancellationToken = default)
    {
        OrderBooks.Add(payload);
        return Task.CompletedTask;
    }

    public Task PublishTradeAsync(string symbol, TradeRecordedRealtimeDto payload, CancellationToken cancellationToken = default)
    {
        Trades.Add(payload);
        return Task.CompletedTask;
    }

    public Task PublishFundsUpdatedAsync(string userId, FundsUpdatedRealtimeDto payload, CancellationToken cancellationToken = default)
    {
        FundsUpdates.Add((userId, payload));
        return Task.CompletedTask;
    }

    public Task PublishSettlementAsync(IEnumerable<string> userIds, SettlementUpdatedRealtimeDto payload, CancellationToken cancellationToken = default)
    {
        foreach (var userId in userIds)
        {
            Settlements.Add((userId, payload));
        }

        return Task.CompletedTask;
    }
}
