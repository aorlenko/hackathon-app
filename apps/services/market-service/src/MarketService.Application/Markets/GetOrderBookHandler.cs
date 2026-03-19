using MarketService.Application.Abstractions;
using MarketService.Domain.Entities;
using Trading.Contracts.Http;

namespace MarketService.Application.Markets;

public sealed class GetOrderBookHandler
{
    private readonly IMarketDataStore _store;

    public GetOrderBookHandler(IMarketDataStore store)
    {
        _store = store;
    }

    public async Task<OrderBookDto> HandleAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var orders = await _store.GetOpenOrdersAsync(symbol, cancellationToken).ConfigureAwait(false);

        var bids = orders.Where(order => order.Side == OrderSide.BUY)
            .GroupBy(order => order.Price)
            .OrderByDescending(group => group.Key)
            .Select(group => new OrderBookLevelDto(group.Key, group.Sum(order => order.RemainingQuantity), group.Count()))
            .ToList();

        var asks = orders.Where(order => order.Side == OrderSide.SELL)
            .GroupBy(order => order.Price)
            .OrderBy(group => group.Key)
            .Select(group => new OrderBookLevelDto(group.Key, group.Sum(order => order.RemainingQuantity), group.Count()))
            .ToList();

        var lastUpdatedAtUtc = await _store.GetLastUpdatedAtUtcAsync(symbol, cancellationToken).ConfigureAwait(false);
        return new OrderBookDto(symbol.ToUpperInvariant(), bids, asks, lastUpdatedAtUtc);
    }
}
