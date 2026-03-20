# Terminal API Contracts — Pet Trading Terminal

Base URL: same host as existing `market-service` (`/api/...`). All routes require the same authenticated user context as the existing pet-trading APIs. The terminal endpoints are additive and do not replace the current listings, snapshot, or history routes used elsewhere.

## Conventions

- Backend remains authoritative for matching, settlement, inventory eligibility, and current market state.
- Money values are JSON numbers with 2-decimal display precision.
- Errors use `ProblemDetails` or the existing market-service error envelope with a user-safe `message`.
- Terminal endpoints resolve the active trader from the authenticated user context; they do not require the client to post a separate `traderId`.

---

## `GET /api/pets/terminal/markets`

Returns the market list shown in the left panel.

**Response 200**

```json
[
  {
    "marketEntryId": "guid",
    "displayName": "Golden Retriever",
    "currentSupply": 3,
    "latestTradePrice": 125.0,
    "bestBidPrice": 121.0,
    "bestAskPrice": 126.0,
    "trendDirection": "Up",
    "lastTradeAt": "2026-03-20T12:00:00Z"
  }
]
```

**Notes**

- `latestTradePrice`, `bestBidPrice`, and `bestAskPrice` may be `null` when the market has no qualifying data.
- Rows are sorted by a stable market-display order chosen by the backend.

---

## `GET /api/pets/terminal/workspace/{marketEntryId}`

Returns the selected market entry state for the center, right, and bottom terminal regions.

**Response 200**

```json
{
  "marketEntry": {
    "marketEntryId": "guid",
    "displayName": "Golden Retriever",
    "currentSupply": 3,
    "latestTradePrice": 125.0,
    "bestBidPrice": 121.0,
    "bestAskPrice": 126.0,
    "trendDirection": "Up",
    "lastTradeAt": "2026-03-20T12:00:00Z"
  },
  "orderBook": {
    "capturedAt": "2026-03-20T12:00:00Z",
    "bids": [
      { "price": 121.0, "quantity": 2, "orderCount": 2 }
    ],
    "asks": [
      { "price": 126.0, "quantity": 1, "orderCount": 1 }
    ]
  },
  "accountSummary": {
    "displayName": "Demo Trader",
    "availableCash": 980.0,
    "lockedCash": 50.0,
    "portfolioTotal": 1225.0,
    "ownedQuantity": 4,
    "eligibleAskQuantity": 3
  },
  "recentTrades": [
    {
      "tradeId": "guid",
      "price": 125.0,
      "quantity": 1,
      "executedAt": "2026-03-20T11:59:58Z",
      "executionType": "BuyNow"
    }
  ],
  "lastUpdatedAt": "2026-03-20T12:00:00Z"
}
```

**Errors**

- `404` if `marketEntryId` is unknown.
- `403` if the authenticated user is not allowed to act as a pet trader.

**Notes**

- `recentTrades` is newest-first.
- `eligibleAskQuantity` counts owned pets of the selected market entry that can be listed right now.
- Empty bid/ask/trade arrays are valid and must drive explicit UI empty states.

---

## `POST /api/pets/terminal/orders/bid`

Submit a terminal bid for the selected market entry.

**Body**

```json
{
  "marketEntryId": "guid",
  "quantity": 2,
  "limitPrice": 121.0
}
```

**Response 200**

```json
{
  "requestId": "guid",
  "action": "PlaceBid",
  "requestedQuantity": 2,
  "filledQuantity": 1,
  "pendingQuantity": 1,
  "rejectedQuantity": 0,
  "averageExecutedPrice": 120.0,
  "message": "1 pet filled immediately; 1 bid is pending below the ask.",
  "affectedTradeIds": ["guid"]
}
```

**Validation**

- `quantity >= 1`
- `limitPrice > 0`
- Backend may immediately fill all or part of the quantity and create pending below-ask bids for any remaining accepted quantity.

---

## `POST /api/pets/terminal/orders/ask`

Submit a terminal ask for owned inventory of the selected market entry.

**Body**

```json
{
  "marketEntryId": "guid",
  "quantity": 2,
  "limitPrice": 130.0
}
```

**Response 200**

```json
{
  "requestId": "guid",
  "action": "PlaceAsk",
  "requestedQuantity": 2,
  "filledQuantity": 0,
  "pendingQuantity": 2,
  "rejectedQuantity": 0,
  "averageExecutedPrice": null,
  "message": "2 pets listed at 130.00.",
  "affectedTradeIds": []
}
```

**Validation**

- Reject when the trader lacks enough eligible owned pets for the selected market entry.
- Backend chooses which eligible pets are listed using a deterministic server-side order.

---

## `POST /api/pets/terminal/orders/buy-now`

Buy from the best currently available asks for the selected market entry.

**Body**

```json
{
  "marketEntryId": "guid",
  "quantity": 2
}
```

**Response 200**

```json
{
  "requestId": "guid",
  "action": "BuyNow",
  "requestedQuantity": 2,
  "filledQuantity": 2,
  "pendingQuantity": 0,
  "rejectedQuantity": 0,
  "averageExecutedPrice": 126.5,
  "message": "Bought 2 pets from the best available asks.",
  "affectedTradeIds": ["guid", "guid"]
}
```

**Validation**

- Backend selects the best asks at submission time using lowest ask first, then oldest listing.
- Reject when the requested quantity cannot be completely satisfied by currently available asks.

---

## Existing endpoint compatibility

The following routes remain valid and continue to support non-terminal pages:

- `GET /api/pets/breeds`
- `GET /api/traders/me/snapshot`
- `GET /api/market/listings`
- `POST /api/market/listings`
- `POST /api/market/listings/{listingId}/bids`
- `GET /api/traders/leaderboard`
- Existing trade and settlement history routes

The terminal page may still reuse these routes internally where practical, but the terminal-specific contracts above are the authoritative Phase 1 design surface for the redesigned workspace.
