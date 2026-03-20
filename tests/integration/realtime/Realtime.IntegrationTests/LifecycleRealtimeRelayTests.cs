using Trading.Contracts.Http;
using Trading.TestSupport;

namespace Realtime.IntegrationTests;

public sealed class LifecycleRealtimeRelayTests
{
    [Fact(Skip = "Equity sell orders disabled (pets-only); matching flow requires seller inventory.")]
    public async Task Relays_trade_and_settlement_updates_to_realtime_publisher()
    {
        var harness = new TradingPlatformHarness();

        await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 8));
        await harness.PlaceOrderAsync("user-2", new PlaceOrderRequest("ABC", OrderSideDto.SELL, 100m, 3));
        var buyer = await harness.GetCurrentAccountSnapshotAsync("user-1");
        var seller = await harness.GetCurrentAccountSnapshotAsync("user-2");

        Assert.NotEmpty(harness.RealtimePublisher.OrderBooks);
        Assert.NotEmpty(harness.RealtimePublisher.Trades);
        Assert.NotEmpty(harness.RealtimePublisher.FundsUpdates);
        Assert.NotEmpty(harness.RealtimePublisher.Settlements);

        Assert.Contains(
            harness.RealtimePublisher.FundsUpdates,
            update => update.UserId == "user-1" && update.Payload.CashAvailable == buyer!.CashAvailable);
        Assert.Contains(
            harness.RealtimePublisher.FundsUpdates,
            update => update.UserId == "user-2" && update.Payload.CashAvailable == seller!.CashAvailable);
    }
}
