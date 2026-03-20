# HTTP API Contracts — Trading Pets (market-service)

Base URL: same host as existing market-service (`/api/...`). All routes **require authorization** unless an explicit demo public read is added (prefer authenticated reads for trader-private data).

**Conventions**

- `traderId` on commands must match **allowed trader** for the current user (see `research.md` R-6).
- Money: **decimal** JSON numbers; currency single **USD** (or unitless “demo dollars”) unless product already defines currency.
- Errors: **ProblemDetails** or consistent `{ "error": "code", "message": "..." }` matching existing market-service style.
- Idempotency: optional `Idempotency-Key` header on POST commands — **nice-to-have**; not required for spec minimum.

---

## Breeds & supply (read-only)

### `GET /api/pets/breeds`

**Response 200**: `BreedDto[]`

```json
{
  "id": "guid",
  "name": "string",
  "category": "Dog | Cat | Bird | Fish",
  "lifespanYears": 12.0,
  "baselineDesirability": 8,
  "maintenanceCost": 0,
  "retailPrice": 120.0,
  "remainingSupply": 3
}
```

(`remainingSupply` is **per-breed primary inventory**; the sample value **3** matches common default supply in `system_reqs.md`—**unrelated** to how many Traders are configured.)

---

## Primary market

### `POST /api/pets/purchase`

**Body**

```json
{
  "traderId": "guid",
  "breedId": "guid",
  "quantity": 2
}
```

**Response 200**: `{ "pets": [ PetSummaryDto ], "availableCash": 0 }`  
**Errors**: 400 insufficient cash/supply; 404 breed.

---

## Trader workspace (private)

### `GET /api/traders/{traderId}/snapshot`

**Response 200**

```json
{
  "traderId": "guid",
  "displayName": "string",
  "availableCash": 0,
  "lockedCash": 0,
  "portfolioTotal": 0,
  "pets": [ PetSummaryDto ],
  "myBids": [ BidStatusDto ]
}
```

`PetSummaryDto` includes fields needed for inventory + analysis link: id, breedName, ageYears, health, currentDesirability, intrinsicValue, isExpired, maintenance (from breed).

### `GET /api/traders/{traderId}/notifications?limit=50`

**Response 200**: `NotificationDto[]` — newest first (see `research.md` R-9).

```json
{
  "id": "guid",
  "type": "BidReceived | BidAccepted | BidRejected | BidWithdrawn | Outbid | ListingRemoved | ...",
  "createdAt": "2026-03-20T12:00:00Z",
  "petId": "guid",
  "petName": "string",
  "amount": 100.0,
  "counterpartyTraderId": "guid",
  "counterpartyDisplayName": "string"
}
```

---

## Secondary market — listings

### `GET /api/market/listings`

Query: optional `sort=createdAtDesc` (default).

**Response 200**: `MarketListingDto[]`

```json
{
  "listingId": "guid",
  "petId": "guid",
  "breedName": "string",
  "askingPrice": 200.0,
  "sellerDisplayName": "string",
  "sellerEmail": "string | null",
  "createdAt": "2026-03-20T12:00:00Z",
  "recentTradePriceForBreed": 150.0,
  "remainingNewSupplyForBreed": 3
}
```

`recentTradePriceForBreed`: **last secondary trade** for that breed, or `null` if none (`research.md` R-2).

### `POST /api/market/listings`

**Body**: `{ "traderId", "petId", "askingPrice" }` — `askingPrice > 0`.

**Response 201**: `{ "listingId" }`  
**Errors**: duplicate active listing, not owner, pet already listed.

### `POST /api/market/listings/{listingId}/withdraw`

**Body**: `{ "traderId" }`  
**Response 204**  
**Effects**: FR-017 (reject pending bid, release cash, notify).

---

## Secondary market — bids

### `POST /api/market/listings/{listingId}/bids`

**Body**: `{ "traderId", "amount" }`

**Behavior** (see `research.md` R-11, spec FR-014–FR-015):

- If `amount >= listing.askingPrice` (**crossing bid**): **Response 200** with **`TradeResultDto`** (or `{ "executed": true, "trade": TradeResultDto }`) — trade completed in this call; listing closed. No seller accept step.
- If `amount < listing.askingPrice`: **Response 201** with `{ "bidId", "status": "Active" }` — pending negotiation.

**Errors**: self-bid, insufficient cash; for **below-ask** bids, new amount must be **>** current active pending bid (equal amount rejected). Crossing bid invalid if listing not active.

### `POST /api/market/bids/{bidId}/withdraw`

**Body**: `{ "traderId" }`  
**Response 204**

---

## Secondary market — seller decisions

### `POST /api/market/listings/{listingId}/accept`

**Body**: `{ "traderId" }` (seller)  
**Response 200**: `TradeResultDto` — pet id, buyer, price.  
**Errors**: **409/400** if there is **no** active **below-ask** bid (e.g. listing already filled or only crossing bids apply).

### `POST /api/market/listings/{listingId}/reject`

**Body**: `{ "traderId" }`  
**Response 204**  
**Errors**: same as accept when no pending below-ask bid.

---

## Analysis

### `GET /api/pets/{petId}/analysis`

Authorization: viewer must be allowed to see pet (owner, or pet on an active listing / public analysis rule — **minimum**: listed pets visible to any authenticated user; owned pets visible only to owner).

**Response 200**: full fundamentals + `intrinsicValue`, `isExpired` (FR-019).

---

## Leaderboard

### `GET /api/traders/leaderboard`

**Response 200**

```json
[
  { "traderId": "guid", "displayName": "string", "portfolioTotal": 0, "rank": 1 }
]
```

Sorted descending by `portfolioTotal` (FR-020).

---

## DTO: `BidStatusDto` (buyer-only projection)

```json
{
  "bidId": "guid",
  "listingId": "guid",
  "petId": "guid",
  "amount": 100.0,
  "status": "Active | Withdrawn | Rejected | Outbid | Accepted"
}
```

---

## Contract tests (mapping)

Each endpoint above SHOULD have at least one contract test in `tests/contract/market-service` covering happy path and one representative error (UX-002: message present in 400 body where applicable).
