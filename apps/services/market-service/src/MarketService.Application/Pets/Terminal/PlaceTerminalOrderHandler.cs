using MarketService.Application.Abstractions;

namespace MarketService.Application.Pets.Terminal;

public sealed class PlaceTerminalOrderHandler
{
    private readonly IMarketPetStore _store;

    public PlaceTerminalOrderHandler(IMarketPetStore store)
    {
        _store = store;
    }

    public Task<TerminalOrderResult?> PlaceBidAsync(
        Guid traderId,
        PlaceTerminalOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0 || request.LimitPrice is null || request.LimitPrice <= 0)
        {
            return Task.FromResult<TerminalOrderResult?>(null);
        }

        return _store.PlaceTerminalBidAsync(
            traderId,
            request.MarketEntryId,
            request.Quantity,
            request.LimitPrice.Value,
            cancellationToken);
    }

    public Task<TerminalOrderResult?> PlaceAskAsync(
        Guid traderId,
        PlaceTerminalOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0 || request.LimitPrice is null || request.LimitPrice <= 0)
        {
            return Task.FromResult<TerminalOrderResult?>(null);
        }

        return _store.PlaceTerminalAskAsync(
            traderId,
            request.MarketEntryId,
            request.Quantity,
            request.LimitPrice.Value,
            cancellationToken);
    }

    public Task<TerminalOrderResult?> BuyNowAsync(
        Guid traderId,
        PlaceTerminalOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return Task.FromResult<TerminalOrderResult?>(null);
        }

        return _store.BuyNowAsync(traderId, request.MarketEntryId, request.Quantity, cancellationToken);
    }
}
