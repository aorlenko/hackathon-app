namespace TradeService.Domain.Entities;

public sealed class Trade
{
    public Guid TradeId { get; set; }
    public Guid BuyOrderId { get; set; }
    public Guid SellOrderId { get; set; }
    public string BuyerUserId { get; set; } = string.Empty;
    public string SellerUserId { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset ExecutedAtUtc { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
