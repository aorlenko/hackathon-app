# Event Contracts (Phase 1)

## Wire Format (All Events)

```json
{
  "eventType": "OrderPlaced",
  "eventVersion": 1,
  "eventId": "uuid",
  "occurredAtUtc": "2026-03-13T10:00:00Z",
  "correlationId": "uuid",
  "producer": "market-service"
}
```

Rules:
- `eventType` + `eventVersion` required for compatibility.
- Events are immutable and append-only.
- Consumers must ignore unknown additive fields.
- Runtime messages are serialized as flat JSON objects matching the CLR event records.
- Service Bus application properties mirror `eventType`, `eventVersion`, `correlationId`, and `producer` for routing/diagnostics.

## Topics and Subscriptions

- Topic: `trading.lifecycle`
  - Subscription: `trade-service.on-order-matched`
  - Subscription: `settlement-service.on-trade-recorded`
  - Subscription: `market-service.relay-realtime` (optional relay)

## `OrderPlaced` (Published by Market Service)

```json
{
  "orderId": "uuid",
  "userId": "auth0|abc",
  "itemId": "uuid",
  "symbol": "ABC",
  "side": "BUY",
  "price": 101.25,
  "quantity": 10,
  "acceptedAtUtc": "2026-03-13T10:00:00Z"
}
```

## `OrderMatched` (Published by Market Service)

```json
{
  "matchId": "uuid",
  "buyOrderId": "uuid",
  "sellOrderId": "uuid",
  "buyerUserId": "auth0|buyer",
  "sellerUserId": "auth0|seller",
  "itemId": "uuid",
  "symbol": "ABC",
  "price": 101.25,
  "quantity": 5,
  "matchedAtUtc": "2026-03-13T10:00:01Z"
}
```

## `TradeRecorded` (Published by Trade Service)

```json
{
  "tradeId": "uuid",
  "buyOrderId": "uuid",
  "sellOrderId": "uuid",
  "buyerUserId": "auth0|buyer",
  "sellerUserId": "auth0|seller",
  "itemId": "uuid",
  "symbol": "ABC",
  "price": 101.25,
  "quantity": 5,
  "executedAtUtc": "2026-03-13T10:00:02Z"
}
```

## `SettlementStarted` (Published by Settlement Service)

```json
{
  "settlementId": "uuid",
  "tradeId": "uuid",
  "buyerUserId": "auth0|buyer",
  "sellerUserId": "auth0|seller",
  "status": "IN_PROGRESS",
  "startedAtUtc": "2026-03-13T10:00:03Z"
}
```

## `SettlementCompleted` (Published by Settlement Service)

```json
{
  "settlementId": "uuid",
  "tradeId": "uuid",
  "buyerUserId": "auth0|buyer",
  "sellerUserId": "auth0|seller",
  "status": "SETTLED",
  "completedAtUtc": "2026-03-13T10:00:05Z",
  "failureReason": null
}
```

`status` may be `SETTLED` or `FAILED` for terminal outcomes.

## Idempotency and Delivery

- Consumers must process by `eventId` idempotently.
- Retries/dead-lettering are enabled by Service Bus subscription policy.
- Trade and settlement consumers are required to be resilient to out-of-order duplicates.
