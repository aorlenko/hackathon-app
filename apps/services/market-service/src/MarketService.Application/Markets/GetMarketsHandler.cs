using MarketService.Application.Abstractions;
using Trading.Contracts.Http;

namespace MarketService.Application.Markets;

public sealed class GetMarketsHandler
{
    private readonly IMarketDataStore _store;

    public GetMarketsHandler(IMarketDataStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<MarketSummaryDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var items = await _store.GetItemsAsync(cancellationToken).ConfigureAwait(false);
        return items
            .OrderBy(item => item.Symbol)
            .Select(item => new MarketSummaryDto(item.Symbol, item.Name, item.Category, item.ReferencePrice, item.IsTradable))
            .ToList();
    }
}
