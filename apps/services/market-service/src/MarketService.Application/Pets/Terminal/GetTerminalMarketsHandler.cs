using MarketService.Application.Abstractions;

namespace MarketService.Application.Pets.Terminal;

public sealed class GetTerminalMarketsHandler
{
    private readonly IMarketPetStore _store;

    public GetTerminalMarketsHandler(IMarketPetStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<TerminalMarketRow>> HandleAsync(CancellationToken cancellationToken = default) =>
        _store.GetTerminalMarketsAsync(cancellationToken);
}
