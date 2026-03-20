using MarketService.Application.Abstractions;

namespace MarketService.Application.Pets;

public sealed class PurchasePetsHandler
{
    private readonly IMarketPetStore _store;

    public PurchasePetsHandler(IMarketPetStore store)
    {
        _store = store;
    }

    public Task<PurchasePetsResult?> HandleAsync(
        Guid traderId,
        Guid breedId,
        int quantity,
        CancellationToken cancellationToken = default) =>
        _store.PurchasePetsAsync(traderId, breedId, quantity, cancellationToken);
}
