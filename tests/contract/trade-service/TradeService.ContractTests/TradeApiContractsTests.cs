using Trading.Contracts.Http;
using Trading.TestSupport;

namespace TradeService.ContractTests;

public sealed class TradeApiContractsTests
{
    [Fact(Skip = "Equity sell orders disabled (pets-only); matching flow requires seller inventory.")]
    public async Task Get_trades_returns_recent_symbol_trades()
    {
        var harness = new TradingPlatformHarness();
        await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 5));
        await harness.PlaceOrderAsync("user-2", new PlaceOrderRequest("ABC", OrderSideDto.SELL, 100m, 5));

        var trades = await harness.GetRecentTrades.ExecuteAsync("ABC", 10);

        Assert.Single(trades);
        Assert.Equal("ABC", trades[0].Symbol);
    }

    [Fact(Skip = "Equity sell orders disabled (pets-only); matching flow requires seller inventory.")]
    public async Task Get_user_trades_returns_user_history()
    {
        var harness = new TradingPlatformHarness();
        await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 5));
        await harness.PlaceOrderAsync("user-2", new PlaceOrderRequest("ABC", OrderSideDto.SELL, 100m, 5));

        var trades = await harness.GetUserTrades.ExecuteAsync("user-1");

        Assert.Single(trades);
        Assert.Equal("user-1", trades[0].BuyerUserId);
    }
}
