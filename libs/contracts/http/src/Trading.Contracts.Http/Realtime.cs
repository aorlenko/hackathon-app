namespace Trading.Contracts.Http;

public sealed record TradeRecordedRealtimeDto(Guid TradeId, string Symbol, decimal Price, int Quantity, DateTimeOffset ExecutedAtUtc, int Version = 1);

public sealed record SettlementUpdatedRealtimeDto(Guid TradeId, string Status, DateTimeOffset ChangedAtUtc, string? FailureReason, int Version = 1);
