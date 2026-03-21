namespace MarketService.Domain.Entities;

public sealed class OrderMatchAudit
{
    public Guid AuditId { get; set; }
    public Guid BuyOrderId { get; set; }
    public Guid SellOrderId { get; set; }
    public decimal MatchPrice { get; set; }
    public int MatchQuantity { get; set; }
    public DateTimeOffset MatchedAtUtc { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
