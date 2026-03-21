namespace MarketService.Domain.Entities;

public enum OrderSide
{
    BUY,
    SELL
}

public enum OrderStatus
{
    OPEN,
    PARTIALLY_FILLED,
    FILLED,
    REJECTED
}

public sealed class Order
{
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int RemainingQuantity { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset AcceptedAtUtc { get; set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUpdatedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }

    public static Order Create(Guid itemId, string symbol, string userId, OrderSide side, decimal price, int quantity, DateTimeOffset acceptedAtUtc)
    {
        return new Order
        {
            OrderId = Guid.NewGuid(),
            ItemId = itemId,
            Symbol = symbol,
            UserId = userId,
            Side = side,
            Price = price,
            Quantity = quantity,
            RemainingQuantity = quantity,
            Status = OrderStatus.OPEN,
            AcceptedAtUtc = acceptedAtUtc,
            LastUpdatedAtUtc = acceptedAtUtc
        };
    }

    public void ApplyFill(int quantity, DateTimeOffset updatedAtUtc)
    {
        RemainingQuantity = Math.Max(0, RemainingQuantity - quantity);
        Status = RemainingQuantity == 0 ? OrderStatus.FILLED : OrderStatus.PARTIALLY_FILLED;
        LastUpdatedAtUtc = updatedAtUtc;
    }

    public void Reject(string reason, DateTimeOffset updatedAtUtc)
    {
        Status = OrderStatus.REJECTED;
        RejectionReason = reason;
        LastUpdatedAtUtc = updatedAtUtc;
    }
}
