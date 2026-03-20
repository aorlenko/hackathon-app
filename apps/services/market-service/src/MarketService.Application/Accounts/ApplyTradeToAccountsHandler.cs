using MarketService.Application.Abstractions;
using Trading.Contracts.Events;
using Trading.Contracts.Http;

namespace MarketService.Application.Accounts;

public sealed class ApplyTradeToAccountsHandler
{
    private readonly IMarketDataStore _store;

    public ApplyTradeToAccountsHandler(IMarketDataStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<FundsUpdatedRealtimeDto>> HandleAsync(
        TradeRecorded trade,
        CancellationToken cancellationToken = default)
    {
        return await HandleAsync(
            trade.BuyerUserId,
            trade.SellerUserId,
            trade.Price,
            trade.Quantity,
            trade.ExecutedAtUtc,
            cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<FundsUpdatedRealtimeDto>> HandleAsync(
        OrderMatched trade,
        CancellationToken cancellationToken = default)
    {
        return HandleAsync(
            trade.BuyerUserId,
            trade.SellerUserId,
            trade.Price,
            trade.Quantity,
            trade.MatchedAtUtc,
            cancellationToken);
    }

    private async Task<IReadOnlyList<FundsUpdatedRealtimeDto>> HandleAsync(
        string buyerUserId,
        string sellerUserId,
        decimal price,
        int quantity,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken)
    {
        var tradeValue = price * quantity;

        await _store.AdjustTraderSpendableCashAsync(buyerUserId, -tradeValue, cancellationToken).ConfigureAwait(false);
        await _store.AdjustTraderSpendableCashAsync(sellerUserId, tradeValue, cancellationToken).ConfigureAwait(false);

        var buyerCash = await _store.GetTraderSpendableCashAsync(buyerUserId, cancellationToken).ConfigureAwait(false);
        var sellerCash = await _store.GetTraderSpendableCashAsync(sellerUserId, cancellationToken).ConfigureAwait(false);

        return
        [
            new FundsUpdatedRealtimeDto(buyerUserId, buyerCash, changedAtUtc),
            new FundsUpdatedRealtimeDto(sellerUserId, sellerCash, changedAtUtc)
        ];
    }
}
