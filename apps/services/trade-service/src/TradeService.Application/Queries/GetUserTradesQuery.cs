using TradeService.Application.Abstractions;
using Trading.Contracts.Http;

namespace TradeService.Application.Queries;

public sealed class GetUserTradesQuery
{
    private readonly ITradeDataStore _store;

    public GetUserTradesQuery(ITradeDataStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<TradeDto>> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var trades = await _store.GetUserTradesAsync(userId, cancellationToken).ConfigureAwait(false);
        return trades
            .Select(trade => new TradeDto(trade.TradeId, trade.Symbol, trade.Price, trade.Quantity, trade.ExecutedAtUtc, trade.BuyerUserId, trade.SellerUserId))
            .ToList();
    }
}
