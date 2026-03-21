# HTTP contracts: Split markets feature (consumer)

The split is **presentation-only**. The SPA remains a **consumer** of the market service. Base URL: `env.marketApiBaseUrl` (`apps/frontend-spa/src/config/env.ts`). Bearer token via `createHeaders(accessToken)` unless noted.

## Endpoints (unchanged from 006 / current trading feature)

| Method | Path | Used on |
|--------|------|---------|
| GET | `/api/traders/me/snapshot` | Both pages (context, inventory, `traderId`) |
| GET | `/api/pets/breeds` | Primary supply market |
| POST | `/api/pets/purchase` | Primary supply market |
| GET | `/api/market/listings` | Resale marketplace (**single feed**; UI splits by `sellerTraderId`) |
| POST | `/api/market/listings` | Resale marketplace (create listing) |
| POST | `/api/market/listings/{listingId}/bids` | Resale marketplace |
| POST | `/api/market/bids/{bidId}/withdraw` | Resale marketplace |
| POST | `/api/market/listings/{listingId}/withdraw` | Resale marketplace |
| POST | `/api/market/listings/{listingId}/accept` | Resale marketplace |
| POST | `/api/market/listings/{listingId}/reject` | Resale marketplace |

SignalR hub: unchanged (`trader.snapshotUpdated`, `market.listingsUpdated`, etc.) — see `useTradingPetsRealtime.ts`.

## JSON shapes

Same as `tradingPetsApi.ts` types: `TraderSnapshotDto`, `BreedDto`, `MarketListingDto`, …

No new fields required for **mine vs others** partitioning: use `MarketListingDto.sellerTraderId` and session `traderId`.

## Compatibility

008 assumes **no breaking changes** to these contracts. If listing volume makes client-side partition insufficient, a **follow-up** feature may add query parameters or dedicated reads; that is **out of scope** for this spec unless product agrees.
