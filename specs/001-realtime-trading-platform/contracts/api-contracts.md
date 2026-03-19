# API Contracts (Phase 1)

## Conventions

- Base path: `/api`
- Auth: Bearer JWT from Auth0 (required except health and public market catalog endpoints)
- Content type: `application/json`
- Error envelope:
  - `code` (string)
  - `message` (string)
  - `details` (object, optional)
  - `correlationId` (string)

## Market Service

### `POST /api/orders`
- **Purpose**: Submit buy/sell order.
- **Request**:
  - `itemSymbol` (string)
  - `side` (`BUY` | `SELL`)
  - `price` (number)
  - `quantity` (integer)
- **Response 202**:
  - `orderId` (string)
  - `status` (`OPEN` | `PARTIALLY_FILLED` | `FILLED`)
  - `acceptedAtUtc` (ISO timestamp)
  - `remainingQuantity` (integer)
- **Errors**:
  - `400` invalid input
  - `401` unauthorized
  - `409` order rejected by rule check (bounds, tradability, balances)

### `GET /api/markets`
- **Purpose**: List tradable items.
- **Response 200**:
  - Array of:
    - `symbol`, `name`, `category`, `referencePrice`, `isTradable`

### `GET /api/markets/{symbol}/order-book`
- **Purpose**: Fetch order book snapshot.
- **Response 200**:
  - `symbol`
  - `bids[]` (`price`, `quantity`, `orderCount`)
  - `asks[]` (`price`, `quantity`, `orderCount`)
  - `lastUpdatedAtUtc`

## Trade Service

### `GET /api/trades`
- **Purpose**: Get recent trades for a symbol.
- **Query**:
  - `symbol` (required)
  - `limit` (optional, default 50, max 200)
- **Response 200**:
  - Array of:
    - `tradeId`, `symbol`, `price`, `quantity`, `executedAtUtc`, `buyerUserId`, `sellerUserId`

### `GET /api/users/{userId}/trades`
- **Purpose**: Get user trade history.
- **Response 200**:
  - Array of trade records sorted desc by execution time.

## Settlement Service

### `GET /api/settlements/{tradeId}`
- **Purpose**: Fetch settlement for a trade.
- **Response 200**:
  - `settlementId`
  - `tradeId`
  - `status` (`PENDING` | `IN_PROGRESS` | `SETTLED` | `FAILED`)
  - `startedAtUtc`
  - `completedAtUtc` (nullable)
  - `failureReason` (nullable)

### `GET /api/users/{userId}/settlements`
- **Purpose**: User-focused settlement history.
- **Response 200**:
  - Array of settlement summaries.

## Realtime Hub Contract

### SignalR Hub: `/hubs/market`

- **Client -> Server**
  - `JoinMarket(symbol)`
  - `LeaveMarket(symbol)`
- **Server -> Client**
  - `OrderBookUpdated(payload)`
  - `TradeRecorded(payload)`
  - `SettlementUpdated(payload)`

Payload DTOs are versioned in shared contract packages (`version` field required).
