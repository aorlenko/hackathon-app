# Data Model: Pet Trading Terminal (005)

## Overview

The terminal feature is primarily a backend-authored read/command layer over the existing pet-trading domain in `market-service`. Most terminal objects are projections or request/response models derived from current breeds, listings, bids, trades, and trader snapshots rather than brand-new persistent aggregates.

---

## Entity: Market Entry

Represents one tradable breed/item row in the terminal market list.

| Field | Type | Notes |
|-------|------|-------|
| MarketEntryId | GUID/string | Same identity as the underlying breed/item |
| Symbol | string | Optional short label if the UI wants a compact market code |
| DisplayName | string | Domain-facing market label shown in the list |
| CurrentSupply | int | Remaining backend-reported supply for the breed/item |
| LatestTradePrice | decimal? | Most recent completed secondary-market trade price |
| BestBidPrice | decimal? | Highest visible bid-side level for the selected market |
| BestAskPrice | decimal? | Lowest visible ask-side level for the selected market |
| TrendDirection | enum | `Up`, `Down`, `Flat`, `NoTradeData` |
| LastTradeAt | datetimeoffset? | Timestamp of the latest completed trade |

**Relationships**: One `MarketEntry` has one active `OrderBookSnapshot`, many `TradeEvent` rows, and one selected presence inside `TradingWorkspaceState`.

**Validation / invariants**

- `CurrentSupply >= 0`.
- `BestBidPrice <= BestAskPrice` is not required because the underlying backend may hold crossing conditions only momentarily between refreshes; the backend-confirmed snapshot wins.
- `TrendDirection` is derived from backend trade history and is never client-authored.

---

## Entity: Order Book Level

One aggregated price level on either the bid or ask side.

| Field | Type | Notes |
|-------|------|-------|
| Side | enum | `Bid` or `Ask` |
| Price | decimal | Positive currency value |
| Quantity | int | Number of pets represented at this level |
| OrderCount | int | Number of underlying listings or bids folded into the level |
| OldestOrderAt | datetimeoffset | Stable tie-break metadata for deterministic ordering |

**Validation / invariants**

- Bid levels sort by `Price DESC`, then `OldestOrderAt ASC`.
- Ask levels sort by `Price ASC`, then `OldestOrderAt ASC`.
- `Quantity > 0` and `OrderCount > 0`.

---

## Entity: Order Book Snapshot

Backend-authored order-book view for the selected market entry.

| Field | Type | Notes |
|-------|------|-------|
| MarketEntryId | GUID/string | Selected breed/item |
| Bids | `OrderBookLevel[]` | Buy-side levels |
| Asks | `OrderBookLevel[]` | Sell-side levels |
| CapturedAt | datetimeoffset | Snapshot time used for highlight comparisons |

**Relationships**: Belongs to one `MarketEntry`; included inside `TradingWorkspaceState`.

**Validation / invariants**

- Empty bid or ask arrays are valid and must render explicit empty states.
- Levels are derived from active backend state only; no speculative client levels exist.

---

## Entity: Trade Event

Completed backend-recorded trade row shown in the terminal feed and used for trend calculations.

| Field | Type | Notes |
|-------|------|-------|
| TradeId | GUID | Stable id for highlighting and dedupe |
| MarketEntryId | GUID/string | Breed/item that traded |
| Price | decimal | Executed trade price |
| Quantity | int | Number of pets represented by the event |
| BuyerTraderId | GUID | Private/account reconciliation use |
| SellerTraderId | GUID | Private/account reconciliation use |
| ExecutedAt | datetimeoffset | Reverse-chronological ordering |
| ExecutionType | enum | `BuyNow`, `CrossingBid`, `AcceptedBid`, `AskCreatedThenFilled`, etc. |

**Validation / invariants**

- Feed ordering is `ExecutedAt DESC`.
- `Quantity > 0`.
- Only completed backend trades appear here.

---

## Entity: Account Summary

Terminal-facing account context rendered in the trading panel.

| Field | Type | Notes |
|-------|------|-------|
| TraderId | GUID | Active signed-in trader |
| DisplayName | string | User/trader label |
| AvailableCash | decimal | Spendable balance |
| LockedCash | decimal | Reserved by active pending bids |
| PortfolioTotal | decimal | Existing trader snapshot field |
| OwnedPets | `OwnedPetSummary[]` | Pets the trader can potentially ask/sell |
| OwnedQuantityByMarketEntry | map | Convenience grouping for terminal quantity controls |

**Relationships**: Included inside `TradingWorkspaceState`; derived from the existing trader snapshot plus grouped inventory.

**Validation / invariants**

- Cash values are copied from backend snapshot and are never locally computed for authoritative display.
- `OwnedQuantityByMarketEntry` must match the underlying `OwnedPets` grouping.

---

## Entity: Owned Pet Summary

Minimal inventory row used by the trading panel when creating asks.

| Field | Type | Notes |
|-------|------|-------|
| PetId | GUID | Underlying owned pet |
| MarketEntryId | GUID/string | Breed/item identity |
| DisplayName | string | Breed/item name |
| IntrinsicValue | decimal | Existing backend field |
| IsEligibleForAsk | bool | False if already listed or otherwise blocked |

---

## Entity: Trading Workspace State

The complete backend-backed payload required to render the terminal page for one selected market entry.

| Field | Type | Notes |
|-------|------|-------|
| SelectedMarketEntry | `MarketEntry` | Active row in the market list |
| MarketEntries | `MarketEntry[]` | List panel content |
| OrderBook | `OrderBookSnapshot` | Center panel |
| AccountSummary | `AccountSummary` | Right panel account/trading context |
| RecentTrades | `TradeEvent[]` | Bottom feed |
| LastUpdatedAt | datetimeoffset | Used for stale-state messaging |

**Validation / invariants**

- The workspace must be internally consistent for a single backend snapshot cycle.
- If refresh fails, the SPA keeps the last confirmed `TradingWorkspaceState` visible and surfaces the error separately.

---

## Entity: Terminal Order Request

Backend command issued by the trading panel.

| Field | Type | Notes |
|-------|------|-------|
| TraderId | GUID | Active trader |
| MarketEntryId | GUID/string | Selected breed/item |
| Action | enum | `PlaceBid`, `PlaceAsk`, `BuyNow` |
| Quantity | int | Positive integer |
| LimitPrice | decimal? | Required for `PlaceBid` and `PlaceAsk`; omitted for `BuyNow` |
| SubmittedAt | datetimeoffset | Server receipt timestamp |

**Validation / invariants**

- `Quantity >= 1`.
- `LimitPrice > 0` when present.
- `BuyNow` ignores client-side price entry and executes against backend-selected best asks.
- Requests are authorized against the current user’s trader identity.

---

## Entity: Terminal Order Result

Normalized backend response that tells the page what happened without exposing speculative intermediate state.

| Field | Type | Notes |
|-------|------|-------|
| RequestId | GUID | Correlation id for user messaging |
| Action | enum | Mirrors request action |
| RequestedQuantity | int | Original quantity |
| FilledQuantity | int | Executed immediately |
| PendingQuantity | int | For below-ask bid quantity left active |
| RejectedQuantity | int | Quantity the backend could not accept |
| AverageExecutedPrice | decimal? | Present when any fills occurred |
| Message | string | User-facing backend-confirmed outcome |
| AffectedTradeIds | GUID[] | New trade ids for immediate reconciliation |

**Validation / invariants**

- `FilledQuantity + PendingQuantity + RejectedQuantity = RequestedQuantity`.
- Results must be safe to display directly to the user.

---

## Relationships (text)

```text
MarketEntry ── 1:1 ── OrderBookSnapshot
MarketEntry ──< TradeEvent
TradingWorkspaceState ── contains ── MarketEntries + SelectedMarketEntry + OrderBook + AccountSummary + RecentTrades
AccountSummary ──< OwnedPetSummary
TerminalOrderRequest ──> TerminalOrderResult
```

---

## State Notes

- `MarketEntry`, `OrderBookSnapshot`, `AccountSummary`, and `TradeEvent[]` are backend projections and may be recomputed frequently.
- Highlight metadata is intentionally not persisted; the SPA derives it from successive confirmed snapshots.
- Existing listing, bid, trade, pet, and trader entities in `market-service` remain the system-of-record behind these terminal models.
