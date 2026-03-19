using MarketService.Application.Matching;
using MarketService.Domain.Entities;

namespace MarketService.UnitTests;

public sealed class MatchingEngineTests
{
    [Fact]
    public void Matches_best_price_then_time_priority()
    {
        var engine = new PriceTimeMatchingEngine();
        var itemId = Guid.NewGuid();
        var earlierSell = Order.Create(itemId, "ABC", "seller-1", OrderSide.SELL, 100m, 5, DateTimeOffset.UtcNow.AddMinutes(-2));
        var laterSell = Order.Create(itemId, "ABC", "seller-2", OrderSide.SELL, 100m, 5, DateTimeOffset.UtcNow.AddMinutes(-1));
        var incomingBuy = Order.Create(itemId, "ABC", "buyer-1", OrderSide.BUY, 105m, 8, DateTimeOffset.UtcNow);

        var matches = engine.Match(incomingBuy, [laterSell, earlierSell], "corr-1", DateTimeOffset.UtcNow);

        Assert.Equal(2, matches.Count);
        Assert.Equal(earlierSell.OrderId, matches[0].SellOrder.OrderId);
        Assert.Equal(5, matches[0].Audit.MatchQuantity);
        Assert.Equal(3, matches[1].Audit.MatchQuantity);
        Assert.Equal(OrderStatus.FILLED, earlierSell.Status);
        Assert.Equal(OrderStatus.FILLED, incomingBuy.Status);
    }

    [Fact]
    public void Supports_partial_fill_and_leaves_remaining_quantity_open()
    {
        var engine = new PriceTimeMatchingEngine();
        var itemId = Guid.NewGuid();
        var restingBuy = Order.Create(itemId, "ABC", "buyer-1", OrderSide.BUY, 101m, 10, DateTimeOffset.UtcNow.AddMinutes(-1));
        var incomingSell = Order.Create(itemId, "ABC", "seller-1", OrderSide.SELL, 100m, 4, DateTimeOffset.UtcNow);

        var matches = engine.Match(incomingSell, [restingBuy], "corr-2", DateTimeOffset.UtcNow);

        Assert.Single(matches);
        Assert.Equal(4, matches[0].Audit.MatchQuantity);
        Assert.Equal(OrderStatus.FILLED, incomingSell.Status);
        Assert.Equal(OrderStatus.PARTIALLY_FILLED, restingBuy.Status);
        Assert.Equal(6, restingBuy.RemainingQuantity);
    }
}
