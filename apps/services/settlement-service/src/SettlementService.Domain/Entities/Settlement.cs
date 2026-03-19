namespace SettlementService.Domain.Entities;

public enum SettlementStatus
{
    PENDING,
    IN_PROGRESS,
    SETTLED,
    FAILED
}

public sealed class Settlement
{
    public Guid SettlementId { get; set; }
    public Guid TradeId { get; set; }
    public string BuyerUserId { get; set; } = string.Empty;
    public string SellerUserId { get; set; } = string.Empty;
    public SettlementStatus Status { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
