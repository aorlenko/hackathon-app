using MarketService.Domain.Entities;

namespace MarketService.Application.Matching;

public sealed record OrderMatchResult(OrderMatchAudit Audit, Order BuyOrder, Order SellOrder);

public sealed class PriceTimeMatchingEngine
{
    public IReadOnlyList<OrderMatchResult> Match(Order incomingOrder, IEnumerable<Order> opposingOrders, string correlationId, DateTimeOffset matchedAtUtc)
    {
        var orderedOpposing = incomingOrder.Side == OrderSide.BUY
            ? opposingOrders.OrderBy(order => order.Price).ThenBy(order => order.AcceptedAtUtc)
            : opposingOrders.OrderByDescending(order => order.Price).ThenBy(order => order.AcceptedAtUtc);

        var results = new List<OrderMatchResult>();

        foreach (var restingOrder in orderedOpposing)
        {
            if (incomingOrder.RemainingQuantity == 0)
            {
                break;
            }

            var priceCompatible = incomingOrder.Side == OrderSide.BUY
                ? incomingOrder.Price >= restingOrder.Price
                : incomingOrder.Price <= restingOrder.Price;

            if (!priceCompatible)
            {
                continue;
            }

            var fillQuantity = Math.Min(incomingOrder.RemainingQuantity, restingOrder.RemainingQuantity);
            if (fillQuantity <= 0)
            {
                continue;
            }

            incomingOrder.ApplyFill(fillQuantity, matchedAtUtc);
            restingOrder.ApplyFill(fillQuantity, matchedAtUtc);

            var buyOrder = incomingOrder.Side == OrderSide.BUY ? incomingOrder : restingOrder;
            var sellOrder = incomingOrder.Side == OrderSide.SELL ? incomingOrder : restingOrder;
            var audit = new OrderMatchAudit
            {
                AuditId = Guid.NewGuid(),
                BuyOrderId = buyOrder.OrderId,
                SellOrderId = sellOrder.OrderId,
                MatchPrice = restingOrder.Price,
                MatchQuantity = fillQuantity,
                MatchedAtUtc = matchedAtUtc,
                CorrelationId = correlationId
            };

            results.Add(new OrderMatchResult(audit, buyOrder, sellOrder));
        }

        return results;
    }
}
