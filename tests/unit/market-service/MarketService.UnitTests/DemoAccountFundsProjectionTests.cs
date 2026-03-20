using MarketService.Application.Accounts;
using Trading.Contracts.Events;
using Trading.TestSupport;

namespace MarketService.UnitTests;

public sealed class DemoAccountFundsProjectionTests
{
    [Fact]
    public async Task Applies_buyer_and_seller_cash_changes_for_confirmed_trade()
    {
        var harness = new TradingPlatformHarness();
        var handler = new ApplyTradeToAccountsHandler(harness.MarketStore);

        var updates = await handler.HandleAsync(CreateTradeRecorded(quantity: 3, price: 100m));
        var buyer = await harness.MarketStore.GetAccountAsync("user-1");
        var seller = await harness.MarketStore.GetAccountAsync("user-2");

        Assert.Equal(2, updates.Count);
        Assert.Equal(249700m, buyer!.CashAvailable);
        Assert.Equal(150300m, seller!.CashAvailable);
    }

    [Fact]
    public async Task Large_trade_transfers_cash_between_wallets()
    {
        var harness = new TradingPlatformHarness();
        var handler = new ApplyTradeToAccountsHandler(harness.MarketStore);

        await handler.HandleAsync(CreateTradeRecorded(quantity: 200, price: 100m));
        var buyer = await harness.MarketStore.GetAccountAsync("user-1");
        var seller = await harness.MarketStore.GetAccountAsync("user-2");

        Assert.Equal(230000m, buyer!.CashAvailable);
        Assert.Equal(170000m, seller!.CashAvailable);
    }

    private static TradeRecorded CreateTradeRecorded(int quantity, decimal price)
    {
        return new TradeRecorded(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 3, 19, 15, 30, 0, TimeSpan.Zero),
            "corr-1",
            "trade-service",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user-1",
            "user-2",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "ABC",
            price,
            quantity,
            new DateTimeOffset(2026, 3, 19, 15, 30, 0, TimeSpan.Zero));
    }
}
