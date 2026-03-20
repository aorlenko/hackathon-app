# Realtime Contracts — Pet Trading Terminal

**Hub path**: `/hubs/market`

The terminal redesign keeps the existing trading-pets SignalR hub model: lightweight invalidation events plus authenticated HTTP refetches. The terminal does not become authoritative from hub payloads alone.

## Client → Server subscriptions

The terminal reuses the existing hub methods already used by the pet-trading SPA:

| Method | Payload | Purpose |
|--------|---------|---------|
| `SubscribeTradingPetsTrader` | `traderId` | Private trader/account invalidation |
| `SubscribeTradingPetsMarket` | none | Shared market/order-book/trade-feed invalidation |
| `SubscribeTradingPetsLeaderboard` | none | Unchanged leaderboard support |

No new hub method is required for minimum-scope terminal delivery.

---

## Server → Client events

Existing event names remain valid:

| Event | Minimal payload | Terminal reaction |
|-------|-----------------|-------------------|
| `trader.snapshotUpdated` | `{ traderId, reason }` | Refetch the selected terminal workspace and active trader summary |
| `market.listingsUpdated` | `{ reason }` | Refetch terminal markets and the selected terminal workspace |
| `trader.notificationsAdded` | `{ traderId, reason }` | Refetch the selected terminal workspace if it belongs to the active trader |
| `leaderboard.updated` | `{ reason }` | No workspace change required; existing leaderboard route behavior stays unchanged |
| `pet.valuationBatch` | `{ petIds, effectiveAt }` | Refetch terminal markets and active workspace if owned or selected market data may have changed |

**Important rule**: The SPA must treat these as invalidation signals only. Rendered values come from the latest successful HTTP terminal response.

---

## Refetch strategy

When the terminal route is open, the client refetches:

1. `GET /api/pets/terminal/markets`
2. `GET /api/pets/terminal/workspace/{marketEntryId}`
3. Any existing account/notification endpoint still needed by other page fragments

The client may coalesce multiple hub events into one refresh cycle, but it must not skip a refresh after a relevant invalidation.

---

## Highlighting behavior

Highlighting is presentation-only and derived after a successful refetch:

- New `recentTrades[].tradeId` values are highlighted as newly appeared.
- Changed best bid, best ask, or last trade prices are highlighted as up/down flashes.
- If the refetch fails, the terminal keeps the last confirmed state visible and surfaces an error without inventing interim data.

---

## Polling fallback

To satisfy the 3-second responsiveness target on the terminal page:

- If the hub is disconnected for more than roughly 3 seconds, begin polling terminal reads every 2 seconds.
- Stop polling as soon as the hub reconnects and subscriptions are restored.
- This tighter fallback is limited to `/pets/workspace`; non-terminal pages can keep their current behavior if unchanged.

---

## Contract verification

Realtime verification must prove:

- Subscriptions still succeed for authenticated pet traders.
- Relevant backend actions cause at least one terminal refresh path to fire.
- Reconnect restores subscriptions.
- Polling fallback converges the page back to backend state when SignalR is unavailable.

The terminal does not require a richer push payload unless later performance evidence shows invalidation-plus-refetch cannot meet `PRF-001`.
