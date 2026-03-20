using Trading.Contracts.Http;
using Trading.TestSupport;

namespace MarketTrade.IntegrationTests;

public sealed class OrderToTradeFlowTests
{
    [Fact(Skip = "Equity sell orders disabled (pets-only); matching flow requires seller inventory.")]
    public async Task Places_opposing_orders_and_records_trade_and_settlement()
    {
        var harness = new TradingPlatformHarness();

        await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 10));
        var sellOutcome = await harness.PlaceOrderAsync("user-2", new PlaceOrderRequest("ABC", OrderSideDto.SELL, 100m, 4));
        var trades = await harness.TradeStore.GetRecentTradesAsync("ABC", 10);
        var settlements = await harness.SettlementStore.GetUserSettlementsAsync("user-1");

        Assert.Equal("FILLED", sellOutcome.Response.Status);
        Assert.Single(trades);
        Assert.Single(settlements);
        Assert.Equal("ABC", trades[0].Symbol);
        Assert.Equal(SettlementService.Domain.Entities.SettlementStatus.SETTLED, settlements[0].Status);
    }

    [Fact(Skip = "Equity sell orders disabled (pets-only); matching flow requires seller inventory.")]
    public async Task Auto_provisioned_users_can_trade_and_settle()
    {
        var harness = new TradingPlatformHarness();

        await harness.PlaceOrderAsync("auth0|buyer-one", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 6));
        var sellOutcome = await harness.PlaceOrderAsync("auth0|seller-two", new PlaceOrderRequest("ABC", OrderSideDto.SELL, 100m, 6));
        var trades = await harness.TradeStore.GetRecentTradesAsync("ABC", 10);
        var settlements = await harness.SettlementStore.GetUserSettlementsAsync("auth0|buyer-one");

        Assert.Equal("FILLED", sellOutcome.Response.Status);
        Assert.Single(trades);
        Assert.Single(settlements);
        Assert.Equal("auth0|buyer-one", trades[0].BuyerUserId);
        Assert.Equal("auth0|seller-two", trades[0].SellerUserId);
        Assert.Equal(SettlementService.Domain.Entities.SettlementStatus.SETTLED, settlements[0].Status);
    }
}
