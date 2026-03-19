# Data Model: Visible User Funds

## 1. Demo Account

Represents the authenticated user's tradable account snapshot and remains the backend source of truth for visible funds in this feature.

### Fields

| Field | Type | Notes |
|---|---|---|
| `userId` | `string` | Stable authenticated user identifier; primary lookup key. |
| `displayName` | `string` | Friendly user-facing name shown in the authenticated shell. |
| `email` | `string` | User email carried through bootstrap/account APIs. |
| `cashAvailable` | `decimal` | Confirmed funds available for new trades; primary value shown in the UI. |
| `holdings` | `Dictionary<string, int>` | Per-symbol quantity map used for sell validation and kept consistent with trade outcomes. |

### Relationships

- One `DemoAccount` belongs to one authenticated user.
- One `DemoAccount` has many holdings keyed by tradable item symbol.
- One `DemoAccount` is updated by many confirmed `Trade Outcome` events over time.

### Validation Rules

- `userId` must be present for all account lookups and realtime routing.
- `cashAvailable` may be zero or negative and must still be displayable.
- Holdings quantities are integer values and may reach zero after sells.

## 2. Available Funds Snapshot

Client-consumable read model for the visible funds area. This is derived from the current `DemoAccount` state and returned by the account query API and realtime updates.

### Fields

| Field | Type | Notes |
|---|---|---|
| `userId` | `string` | Must match the authenticated session account. |
| `cashAvailable` | `decimal` | Latest confirmed amount shown prominently. |
| `changedAtUtc` | `DateTimeOffset` | Timestamp of the most recent confirmed balance-changing update or snapshot freshness marker. |
| `holdings` | `collection` | Included in the snapshot for consistency with existing account contract and future read needs. |

### Relationships

- One `Available Funds Snapshot` is derived from one `DemoAccount`.
- A new snapshot is emitted after successful account bootstrap, explicit account refresh, or confirmed trade-driven account mutation.

### Validation Rules

- Snapshot values must reflect persisted backend state, not optimistic client assumptions.
- Snapshot timestamps must move forward when confirmed funds changes are emitted.

## 3. Trade Outcome

Represents the confirmed trade result that can affect buyer and seller funds.

### Fields

| Field | Type | Notes |
|---|---|---|
| `tradeId` | `Guid` | Unique trade identifier. |
| `buyerUserId` | `string` | User whose available funds decrease and holdings increase. |
| `sellerUserId` | `string` | User whose available funds increase and holdings decrease. |
| `symbol` | `string` | Traded item symbol used for holdings updates. |
| `price` | `decimal` | Matched price. |
| `quantity` | `int` | Matched quantity. |
| `executedAtUtc` | `DateTimeOffset` | Trade confirmation timestamp from `TradeRecorded`. |

### Relationships

- One `Trade Outcome` affects up to two `DemoAccount` records: buyer and seller.
- One `Trade Outcome` may trigger one or two `FundsUpdated` realtime messages depending on connected users.

### Validation Rules

- Funds updates are applied only for confirmed trade outcomes.
- Rejected, cancelled, or otherwise non-effective trade attempts do not create a `Trade Outcome` and therefore do not mutate visible funds.

## 4. Funds Display State

Frontend-only presentation state for the header summary.

### States

| State | Meaning | UI Behavior |
|---|---|---|
| `loading` | Initial authenticated fetch is in progress and no confirmed amount is available yet. | Show skeleton or loading label instead of an empty space. |
| `confirmed` | Latest visible amount is current and confirmed. | Show formatted funds amount with neutral/healthy label. |
| `updating` | A fresher amount is being retrieved after reconnect, order lifecycle progress, or delayed event handling. | Keep the last confirmed amount visible and show an updating indicator. |
| `unavailable` | The current amount could not be refreshed or confirmed. | Keep the last confirmed amount visible if present, plus retry/wait guidance. |

### State Transitions

- `loading -> confirmed`: initial account snapshot succeeds.
- `loading -> unavailable`: initial account snapshot fails.
- `confirmed -> updating`: a refresh or expected post-trade update is pending.
- `updating -> confirmed`: refreshed snapshot or realtime funds update succeeds.
- `updating -> unavailable`: refresh exceeds the delay threshold or fails.
- `unavailable -> updating`: user retries or reconnect recovery starts.
- `unavailable -> confirmed`: retry succeeds with a confirmed snapshot.

## 5. Funds Updated Realtime Message

User-scoped event sent over the existing market hub when confirmed funds change.

### Fields

| Field | Type | Notes |
|---|---|---|
| `userId` | `string` | Target account owner for the update. |
| `cashAvailable` | `decimal` | Latest confirmed visible funds amount. |
| `changedAtUtc` | `DateTimeOffset` | Server-confirmed time of the applied update. |
| `version` | `int` | Contract version for forward-compatible evolution. |

### Relationships

- Produced after the market-service account projection persists a confirmed balance change.
- Consumed by the frontend account state layer to move the display back to `confirmed`.

### Validation Rules

- Event payloads must be published only after persistence succeeds.
- Event payloads must match the user/account snapshot the UI would retrieve from `GET /api/accounts/me`.
