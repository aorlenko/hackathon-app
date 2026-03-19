using TradeService.Application.Abstractions;
using Trading.Contracts.Http;

namespace TradeService.Application.Queries;

public sealed class GetRecentTradesQuery
{
    private readonly ITradeDataStore _store;

    public GetRecentTradesQuery(ITradeDataStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<TradeDto>> ExecuteAsync(string symbol, int limit = 50, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 200);
        var trades = await _store.GetRecentTradesAsync(symbol, limit, cancellationToken).ConfigureAwait(false);
        return trades.Select(Map).ToList();
    }

    private static TradeDto Map(Domain.Entities.Trade trade) => new(trade.TradeId, trade.Symbol, trade.Price, trade.Quantity, trade.ExecutedAtUtc, trade.BuyerUserId, trade.SellerUserId);
}
