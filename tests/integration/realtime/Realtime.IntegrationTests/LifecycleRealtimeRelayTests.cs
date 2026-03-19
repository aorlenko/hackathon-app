using Trading.Contracts.Http;
using Trading.TestSupport;

namespace Realtime.IntegrationTests;

public sealed class LifecycleRealtimeRelayTests
{
    [Fact]
    public async Task Relays_trade_and_settlement_updates_to_realtime_publisher()
    {
        var harness = new TradingPlatformHarness();

        await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 8));
        await harness.PlaceOrderAsync("user-2", new PlaceOrderRequest("ABC", OrderSideDto.SELL, 100m, 3));

        Assert.NotEmpty(harness.RealtimePublisher.OrderBooks);
        Assert.NotEmpty(harness.RealtimePublisher.Trades);
        Assert.NotEmpty(harness.RealtimePublisher.Settlements);
    }
}
