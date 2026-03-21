# HTTP contracts: Pet Trading workspace (consumer)

The workspace is a **consumer** of the market service. Base URL: `env.marketApiBaseUrl` (see `apps/frontend-spa/src/config/env.ts`). All calls use bearer token via `createHeaders(accessToken)` unless noted.

## Endpoints used by the workspace

| Method | Path | Purpose |
|--------|------|---------|
| GET | `/api/traders/me/snapshot` | Resolve trader + financial summary + `pets[]` + `myBids[]` |
| GET | `/api/pets/breeds` | Primary market breed rows (`retailPrice`, `remainingSupply`) |
| POST | `/api/pets/purchase` | Buy from primary supply |
| GET | `/api/market/listings` | Secondary market open listings |
| POST | `/api/market/listings` | Create listing |
| POST | `/api/market/listings/{listingId}/bids` | Place bid |
| POST | `/api/market/bids/{bidId}/withdraw` | Withdraw bid |
| POST | `/api/market/listings/{listingId}/withdraw` | Withdraw listing |
| POST | `/api/market/listings/{listingId}/accept` | Accept bid |
| POST | `/api/market/listings/{listingId}/reject` | Reject bid |
| GET | `/api/traders/{traderId}/notifications?limit={n}` | Activity feed |

## Representative JSON shapes (TypeScript-aligned)

Types are defined in `tradingPetsApi.ts`. Summary:

### `TraderSnapshotDto`

- `traderId`, `displayName`
- `availableCash`, `lockedCash`, `portfolioTotal` (numbers)
- `pets`: `PetSummaryDto[]`
- `myBids`: array of `{ bidId, listingId, petId, amount, status }`

### `BreedDto`

- `id`, `name`, `category`, `lifespanYears`, `baselineDesirability`, `maintenanceCost`, `retailPrice`, `remainingSupply`

### `MarketListingDto`

- `listingId`, `petId`, `sellerTraderId`, `breedName`, `askingPrice`, `sellerDisplayName`, `sellerEmail?`, `createdAt`, `recentTradePriceForBreed`, `remainingNewSupplyForBreed`

### `NotificationDto`

- `id`, `type` (e.g. `BidReceived`, `TradeCompleted`, …), `createdAt`, `petId`, `petName`, `amount`, `counterpartyTraderId`, `counterpartyDisplayName`

## Versioning and compatibility

006 UX work assumes **no breaking changes** to these contracts. If the UI requires a field not present in DTOs, escalate as spec A-002 exception (smallest possible server change).
