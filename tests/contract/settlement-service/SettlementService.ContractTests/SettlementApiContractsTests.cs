using Trading.Contracts.Http;
using Trading.TestSupport;

namespace SettlementService.ContractTests;

public sealed class SettlementApiContractsTests
{
    [Fact(Skip = "Equity sell orders disabled (pets-only); matching flow requires seller inventory.")]
    public async Task Get_settlement_by_trade_returns_status_projection()
    {
        var harness = new TradingPlatformHarness();
        await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 5));
        await harness.PlaceOrderAsync("user-2", new PlaceOrderRequest("ABC", OrderSideDto.SELL, 100m, 5));
        var tradeId = (await harness.TradeStore.GetRecentTradesAsync("ABC", 10)).Single().TradeId;

        var settlement = await harness.GetSettlementByTrade.ExecuteAsync(tradeId);

        Assert.NotNull(settlement);
        Assert.Equal(tradeId, settlement!.TradeId);
        Assert.Equal("SETTLED", settlement.Status);
    }

    [Fact(Skip = "Equity sell orders disabled (pets-only); matching flow requires seller inventory.")]
    public async Task Get_user_settlements_returns_user_history()
    {
        var harness = new TradingPlatformHarness();
        await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 5));
        await harness.PlaceOrderAsync("user-2", new PlaceOrderRequest("ABC", OrderSideDto.SELL, 100m, 5));

        var settlements = await harness.GetUserSettlements.ExecuteAsync("user-1");

        Assert.Single(settlements);
        Assert.Equal("user-1", settlements[0].BuyerUserId);
    }
}
