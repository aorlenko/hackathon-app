namespace Trading.Contracts.Events;

public interface ITradingEvent
{
    Guid EventId { get; }
    string EventType { get; }
    int EventVersion { get; }
    DateTimeOffset OccurredAtUtc { get; }
    string CorrelationId { get; }
    string Producer { get; }
}

public abstract record TradingEventBase(
    Guid EventId,
    string EventType,
    int EventVersion,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    string Producer) : ITradingEvent;

public sealed record OrderPlaced(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    string Producer,
    Guid OrderId,
    string UserId,
    Guid ItemId,
    string Symbol,
    string Side,
    decimal Price,
    int Quantity,
    DateTimeOffset AcceptedAtUtc)
    : TradingEventBase(EventId, nameof(OrderPlaced), 1, OccurredAtUtc, CorrelationId, Producer);

public sealed record OrderMatched(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    string Producer,
    Guid MatchId,
    Guid BuyOrderId,
    Guid SellOrderId,
    string BuyerUserId,
    string SellerUserId,
    Guid ItemId,
    string Symbol,
    decimal Price,
    int Quantity,
    DateTimeOffset MatchedAtUtc)
    : TradingEventBase(EventId, nameof(OrderMatched), 1, OccurredAtUtc, CorrelationId, Producer);

public sealed record TradeRecorded(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    string Producer,
    Guid TradeId,
    Guid BuyOrderId,
    Guid SellOrderId,
    string BuyerUserId,
    string SellerUserId,
    Guid ItemId,
    string Symbol,
    decimal Price,
    int Quantity,
    DateTimeOffset ExecutedAtUtc)
    : TradingEventBase(EventId, nameof(TradeRecorded), 1, OccurredAtUtc, CorrelationId, Producer);

public sealed record SettlementStarted(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    string Producer,
    Guid SettlementId,
    Guid TradeId,
    string BuyerUserId,
    string SellerUserId,
    string Status,
    DateTimeOffset StartedAtUtc)
    : TradingEventBase(EventId, nameof(SettlementStarted), 1, OccurredAtUtc, CorrelationId, Producer);

public sealed record SettlementCompleted(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    string Producer,
    Guid SettlementId,
    Guid TradeId,
    string BuyerUserId,
    string SellerUserId,
    string Status,
    DateTimeOffset CompletedAtUtc,
    string? FailureReason)
    : TradingEventBase(EventId, nameof(SettlementCompleted), 1, OccurredAtUtc, CorrelationId, Producer);
