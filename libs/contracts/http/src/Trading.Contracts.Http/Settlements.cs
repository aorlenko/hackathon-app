namespace Trading.Contracts.Http;

public sealed record SettlementDto(
    Guid SettlementId,
    Guid TradeId,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? FailureReason,
    string BuyerUserId,
    string SellerUserId);
