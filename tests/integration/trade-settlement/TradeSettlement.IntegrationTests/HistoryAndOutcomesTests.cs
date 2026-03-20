using Trading.Contracts.Http;
using Trading.TestSupport;

namespace TradeSettlement.IntegrationTests;

public sealed class HistoryAndOutcomesTests
{
    [Fact(Skip = "Equity sell orders disabled (pets-only); matching flow requires seller inventory.")]
    public async Task Retrieves_trade_history_and_settlement_outcomes_after_match()
    {
        var harness = new TradingPlatformHarness();
        await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 7));
        await harness.PlaceOrderAsync("user-2", new PlaceOrderRequest("ABC", OrderSideDto.SELL, 100m, 7));

        var userTrades = await harness.GetUserTrades.ExecuteAsync("user-1");
        var userSettlements = await harness.GetUserSettlements.ExecuteAsync("user-1");

        Assert.Single(userTrades);
        Assert.Single(userSettlements);
        Assert.Equal(userTrades[0].TradeId, userSettlements[0].TradeId);
        Assert.Equal("SETTLED", userSettlements[0].Status);
    }
}
