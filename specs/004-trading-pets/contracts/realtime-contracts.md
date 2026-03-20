# SignalR Realtime Contracts — Trading Pets

**Hub path**: `/hubs/market` (existing `MarketService.Api.Hubs.MarketHub` — trading-pets groups and methods are additive).

**Client → server (hub methods)**

| Method | Payload | Group membership |
|--------|---------|------------------|
| `SubscribeTradingPetsTrader` | `traderId` (Guid) | `tp:trader:{traderId}` |
| `UnsubscribeTradingPetsTrader` | `traderId` (Guid) | removes from `tp:trader:{traderId}` |
| `SubscribeTradingPetsMarket` | — | `tp:market` (`MarketHub.TradingPetsMarketGroup`) |
| `SubscribeTradingPetsLeaderboard` | — | `tp:leaderboard` (`MarketHub.TradingPetsLeaderboardGroup`) |

**Server → client (method names)**

| Event | Payload (minimal) |
|-------|-------------------|
| `trader.snapshotUpdated` | `{ traderId, reason }` — client should refetch `GET /api/traders/{traderId}/snapshot` |
| `market.listingsUpdated` | `{ reason }` — refetch `GET /api/market/listings` |
| `trader.notificationsAdded` | `{ traderId, reason }` — refetch `GET /api/traders/{traderId}/notifications` |
| `leaderboard.updated` | `{ reason }` — refetch `GET /api/traders/leaderboard` |
| `pet.valuationBatch` | `{ petIds, effectiveAt }` — refetch snapshot/analysis as needed |

**Auth**: Same JWT bearer as HTTP; connection identifies user; server maps to allowed **Trader** ids and filters events (never push another trader’s private snapshot).

---

## Client → Server (optional)

| Method | Payload | Purpose |
|--------|---------|---------|
| `SubscribeTrader` | `{ traderId }` | Register for private trader streams |
| `UnsubscribeTrader` | `{ traderId }` | Tear down |
| `SubscribeMarket` | `{ }` | Market listing stream |
| `SubscribeLeaderboard` | `{ }` | Leaderboard updates |

If simplification desired: auto-subscribe all allowed traders for the connection on connect — document in implementation.

---

## Server → Client events

Payloads are **JSON**; use **camelCase** to match typical ASP.NET SignalR JSON protocol.

### `trader.snapshotUpdated`

**When**: After purchase, bid lock change, **instant cross trade**, trade settlement, listing withdraw affecting that trader, or valuation tick touching owned pets.

```json
{
  "traderId": "guid",
  "snapshot": { }
}
```

`snapshot` shape aligns with `GET /api/traders/{id}/snapshot` (compact variant allowed if documented).

### `market.listingsUpdated`

**When**: New listing, withdraw, or post-trade removal of listing.

```json
{
  "listings": [ { } ]
}
```

Full list or **patch** strategy — if patch, document version vectors; **simplest**: push “invalidate” + client refetch.

**Simplest compliant approach (recommended for demo)**: send `{ "reason": "listingChanged" }` and have client **refetch** `GET /api/market/listings` — still meets PRF-001 if refetch completes within budget.

### `trader.notificationsAdded`

**When**: New notification rows for trader.

```json
{
  "traderId": "guid",
  "notifications": [ NotificationDto ]
}
```

### `leaderboard.updated`

**When**: Any trade or valuation tick changes portfolio totals.

```json
{
  "rows": [ { "traderId", "displayName", "portfolioTotal", "rank" } ]
}
```

### `pet.valuationBatch`

**When**: After scheduled tick (FR-008) — optional optimization to drive analysis view.

```json
{
  "petIds": [ "guid" ],
  "effectiveAt": "2026-03-20T12:01:00Z"
}
```

Clients refetch analysis or snapshot as needed.

---

## Performance note (PRF-001)

- Prefer **coalescing** valuation broadcasts (one `pet.valuationBatch` per tick per affected set) over per-pet messages.
- Ensure **UI applies updates within a few seconds** of server commit; measure with demo script (PRF-002).

---

## Fallback (PRF-003)

When SignalR unavailable, SPA uses **HTTP polling** (see `quickstart.md`) at a fixed interval so panels do not stay stale indefinitely.
