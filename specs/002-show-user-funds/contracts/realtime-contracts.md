# Realtime Contracts: Visible User Funds

## Overview

The feature extends the existing `/hubs/market` SignalR connection with a user-scoped funds update message so the UI can reflect confirmed balance changes without polling.

## 1. Hub Transport

- **Hub**: `/hubs/market`
- **Connection model**: Reuse the existing frontend `createMarketHubClient()` connection and automatic reconnect behavior.
- **Scope**: User-scoped account updates are sent only to the authenticated user's account group.

## 2. New Server-to-Client Event

### `FundsUpdated`

- **Purpose**: Notify the frontend that the authenticated user's confirmed available funds changed.
- **Delivery rule**: Published only after the backend has persisted the updated account snapshot.
- **Payload**:

```json
{
  "version": 1,
  "payload": {
    "userId": "user-1",
    "cashAvailable": 9950.0,
    "changedAtUtc": "2026-03-19T15:30:00Z"
  }
}
```

## 3. Suggested Shared DTO Shape

```json
{
  "version": 1,
  "payload": {
    "userId": "string",
    "cashAvailable": "decimal",
    "changedAtUtc": "date-time"
  }
}
```

## 4. Trigger Rules

- A confirmed buy trade decreases the buyer's `cashAvailable` and emits `FundsUpdated` for the buyer.
- A confirmed sell trade increases the seller's `cashAvailable` and emits `FundsUpdated` for the seller.
- If a single trade affects two authenticated users, each user receives a user-specific funds update reflecting only their account.
- Rejected, cancelled, or otherwise non-effective trade attempts must not emit `FundsUpdated`.

## 5. Client Handling Rules

- On `FundsUpdated`, the client updates the stored account snapshot and transitions the display state to `confirmed`.
- If the client reconnects after a disconnect, it should refresh `GET /api/accounts/me` to confirm no updates were missed.
- If a refresh is pending and no confirmed amount is available yet, the display remains in `loading`.
- If a refresh is pending and a prior confirmed amount exists, the display remains visible with `updating`.

## 6. Compatibility Notes

- Existing realtime events remain unchanged:
  - `OrderBookUpdated`
  - `TradeRecorded`
  - `SettlementUpdated`
- `FundsUpdated` is additive and should follow the same envelope/versioning pattern already used in the frontend contracts.
