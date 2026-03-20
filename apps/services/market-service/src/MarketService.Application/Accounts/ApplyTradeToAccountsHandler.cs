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
            trade.Symbol,
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
            trade.Symbol,
            trade.Price,
            trade.Quantity,
            trade.MatchedAtUtc,
            cancellationToken);
    }

    private async Task<IReadOnlyList<FundsUpdatedRealtimeDto>> HandleAsync(
        string buyerUserId,
        string sellerUserId,
        string symbol,
        decimal price,
        int quantity,
        DateTimeOffset changedAtUtc,
        CancellationToken cancellationToken)
    {
        var buyerAccount = await _store.GetAccountAsync(buyerUserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Could not load buyer account for {buyerUserId}.");
        var sellerAccount = await _store.GetAccountAsync(sellerUserId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Could not load seller account for {sellerUserId}.");

        var tradeValue = price * quantity;

        buyerAccount.CashAvailable -= tradeValue;
        buyerAccount.Holdings[symbol] = buyerAccount.Holdings.TryGetValue(symbol, out var buyerHolding)
            ? buyerHolding + quantity
            : quantity;

        if (!sellerAccount.Holdings.TryGetValue(symbol, out var sellerHolding))
        {
            throw new InvalidOperationException($"Could not find seller holding for {sellerUserId} and {symbol}.");
        }

        sellerAccount.CashAvailable += tradeValue;
        var remainingSellerHolding = sellerHolding - quantity;
        if (remainingSellerHolding > 0)
        {
            sellerAccount.Holdings[symbol] = remainingSellerHolding;
        }
        else
        {
            sellerAccount.Holdings.Remove(symbol);
        }

        await _store.SaveAccountsAsync([buyerAccount, sellerAccount], cancellationToken).ConfigureAwait(false);

        return
        [
            new FundsUpdatedRealtimeDto(buyerAccount.UserId, buyerAccount.CashAvailable, changedAtUtc),
            new FundsUpdatedRealtimeDto(sellerAccount.UserId, sellerAccount.CashAvailable, changedAtUtc)
        ];
    }
}
