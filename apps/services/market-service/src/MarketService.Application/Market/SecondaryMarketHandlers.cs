using MarketService.Application.Abstractions;

namespace MarketService.Application.Market;

public sealed class SecondaryMarketHandlers
{
    private readonly IMarketPetStore _store;

    public SecondaryMarketHandlers(IMarketPetStore store)
    {
        _store = store;
    }

    public Task<Guid?> CreateListingAsync(
        Guid traderId,
        Guid petId,
        decimal askingPrice,
        CancellationToken cancellationToken = default) =>
        _store.CreateListingAsync(traderId, petId, askingPrice, cancellationToken);

    public Task<bool> WithdrawListingAsync(
        Guid traderId,
        Guid listingId,
        CancellationToken cancellationToken = default) =>
        _store.WithdrawListingAsync(traderId, listingId, cancellationToken);

    public Task<PlaceBidResult?> PlaceBidAsync(
        Guid traderId,
        Guid listingId,
        decimal amount,
        CancellationToken cancellationToken = default) =>
        _store.PlaceBidAsync(traderId, listingId, amount, cancellationToken);

    public Task<bool> WithdrawBidAsync(
        Guid traderId,
        Guid bidId,
        CancellationToken cancellationToken = default) =>
        _store.WithdrawBidAsync(traderId, bidId, cancellationToken);

    public Task<TradeResultRow?> AcceptBidAsync(
        Guid traderId,
        Guid listingId,
        CancellationToken cancellationToken = default) =>
        _store.AcceptBidAsync(traderId, listingId, cancellationToken);

    public Task<(bool ok, Guid? buyerTraderId)> RejectBidAsync(
        Guid traderId,
        Guid listingId,
        CancellationToken cancellationToken = default) =>
        _store.RejectBidAsync(traderId, listingId, cancellationToken);
}
