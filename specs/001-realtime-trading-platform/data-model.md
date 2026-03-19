# Phase 1 Data Model: Simplified Real-Time Trading Platform

## Modeling Approach

- Azure SQL Database with EF Core migrations and default-schema tables.
- Immutable event identifiers and timestamps on lifecycle records.
- Soft constraints optimized for demo speed, with explicit validation rules in service layer.

## Market-Owned Tables

### `DemoAccounts`
- **Purpose**: Simulated participant balances used for market validation.
- **Fields**:
  - `user_id` (PK, string, Auth0 subject)
  - `display_name` (nvarchar)
  - `email` (nvarchar)
  - `cash_available` (decimal(18,2))
- **Validation**:
  - `user_id` required and unique.
  - email format validated at API boundary.

### `DemoHoldings`
- **Purpose**: Per-user seeded holdings used for sell-side validation.
- **Fields**:
  - `user_id` (FK -> `DemoAccounts.user_id`)
  - `symbol` (nvarchar)
  - `quantity` (int)
- **Validation**:
  - quantity non-negative.
  - one row per `(user_id, symbol)`.

### `Items`
- **Purpose**: Tradable instrument catalog.
- **Fields**:
  - `item_id` (PK, uniqueidentifier)
  - `symbol` (nvarchar, unique)
  - `name` (nvarchar)
  - `category` (nvarchar)
  - `reference_price` (decimal(18,4))
  - `is_tradable` (bit)
  - `created_at_utc` (datetime2)
- **Validation**:
  - `symbol`, `name` required.
  - `reference_price > 0`.

### `Orders`
- **Purpose**: Order intents and current open/terminal state.
- **Fields**:
  - `order_id` (PK, uniqueidentifier)
  - `user_id` (string)
  - `item_id` (FK -> `Items.item_id`)
  - `side` (nvarchar: BUY|SELL)
  - `price` (decimal(18,4))
  - `quantity` (int)
  - `remaining_quantity` (int)
  - `status` (nvarchar: OPEN|PARTIALLY_FILLED|FILLED|REJECTED)
  - `accepted_at_utc` (datetime2)
  - `last_updated_at_utc` (datetime2)
  - `rejection_reason` (nvarchar, nullable)
- **Validation**:
  - `price > 0`, `quantity > 0`.
  - `remaining_quantity` between 0 and `quantity`.
  - reject if item not tradable or account constraints fail.

### `OrderMatchAudits`
- **Purpose**: Traceability for matching decisions.
- **Fields**:
  - `audit_id` (PK, uniqueidentifier)
  - `buy_order_id` (uniqueidentifier)
  - `sell_order_id` (uniqueidentifier)
  - `match_price` (decimal(18,4))
  - `match_quantity` (int)
  - `matched_at_utc` (datetime2)
  - `correlation_id` (nvarchar)

## Trade-Owned Tables

### `Trades`
- **Purpose**: Immutable trade executions.
- **Fields**:
  - `trade_id` (PK, uniqueidentifier)
  - `buy_order_id` (uniqueidentifier)
  - `sell_order_id` (uniqueidentifier)
  - `buyer_user_id` (string)
  - `seller_user_id` (string)
  - `item_id` (uniqueidentifier)
  - `price` (decimal(18,4))
  - `quantity` (int)
  - `executed_at_utc` (datetime2)
  - `correlation_id` (nvarchar)
- **Validation**:
  - quantity and price positive.
  - trade records are append-only (no updates except metadata corrections if needed).

## Settlement-Owned Tables

### `Settlements`
- **Purpose**: Settlement workflow state per trade.
- **Fields**:
  - `settlement_id` (PK, uniqueidentifier)
  - `trade_id` (uniqueidentifier, unique)
  - `status` (nvarchar: PENDING|IN_PROGRESS|SETTLED|FAILED)
  - `started_at_utc` (datetime2)
  - `completed_at_utc` (datetime2, nullable)
  - `failure_reason` (nvarchar, nullable)
  - `policy_snapshot_json` (nvarchar(max))
  - `correlation_id` (nvarchar)
- **Validation**:
  - terminal states are `SETTLED` or `FAILED`.
  - `completed_at_utc` required for terminal states.

## Relationships

- `DemoAccounts` 1 -> n `DemoHoldings`
- `Items` 1 -> n `Orders`
- `Orders` n -> n via matching pairs in `OrderMatchAudits`
- `Trades` references matched order pairs
- `Trades` 1 -> 1 `Settlements`

## State Transitions

### Order
- `OPEN -> PARTIALLY_FILLED -> FILLED`
- `OPEN -> REJECTED`
- Guard: transitions driven by matching outcomes and validation.

### Settlement
- `PENDING -> IN_PROGRESS -> SETTLED`
- `PENDING|IN_PROGRESS -> FAILED`
- Guard: configurable policy checks and simulated transfer execution.

## First Implementation Slice

Prioritize these tables/entities first:
1. `DemoAccounts`, `DemoHoldings`, `Items` (seed baseline)
2. `Orders`
3. `Trades`
4. `Settlements`

Add `OrderMatchAudits` immediately after first vertical slice passes.
