using MarketService.Application.Abstractions;

namespace MarketService.Application.Pets.Terminal;

public sealed class GetTerminalWorkspaceHandler
{
    private readonly IMarketPetStore _store;

    public GetTerminalWorkspaceHandler(IMarketPetStore store)
    {
        _store = store;
    }

    public Task<TerminalWorkspaceSnapshot?> HandleAsync(
        Guid traderId,
        Guid marketEntryId,
        CancellationToken cancellationToken = default) =>
        _store.GetTerminalWorkspaceAsync(traderId, marketEntryId, cancellationToken);
}
