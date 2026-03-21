# Data model: Pet Trading workspace (client view)

This feature does **not** introduce new server tables. Below maps **spec entities** to **existing API DTOs** consumed in `apps/frontend-spa/src/features/trading-pets/tradingPetsApi.ts`.

## Trader workspace summary

| Concept (spec) | Source | Fields |
|----------------|--------|--------|
| Available cash | `TraderSnapshotDto` | `availableCash` |
| Locked cash | `TraderSnapshotDto` | `lockedCash` |
| Portfolio total | `TraderSnapshotDto` | `portfolioTotal` |
| Trader identity | `TraderSnapshotDto` | `traderId`, `displayName` |

**Validation**: Read-only display; values must match `GET /api/traders/me/snapshot` (FR-001, FR-007).

## Primary supply offer (per breed)

| Concept (spec) | Source | Fields |
|----------------|--------|--------|
| Breed catalog | `BreedDto[]` | `id`, `name`, `category`, … |
| Retail price | `BreedDto` | `retailPrice` |
| Remaining supply | `BreedDto` | `remainingSupply` |
| Purchase action | `POST /api/pets/purchase` | body: `traderId`, `breedId`, `quantity` |

**State**: Supply and cash update after successful purchase + refetch (`getBreeds`, `getMyTraderSnapshot` / parent `refresh`).

## Resale listing

| Concept (spec) | Source | Fields |
|----------------|--------|--------|
| Open listings | `MarketListingDto` | `listingId`, `petId`, `breedName`, `askingPrice`, `sellerDisplayName`, `sellerEmail?`, `createdAt`, … |
| List pet | `POST /api/market/listings` | `traderId`, `petId`, `askingPrice` |
| Bid | `POST /api/market/listings/{id}/bids` | `traderId`, `amount` |
| Withdraw listing / bid | existing endpoints | see `tradingPetsApi.ts` |
| Seller accept/reject | existing endpoints | `acceptBid`, `rejectBid` |

**Relationships**: Listing references `petId`; viewer’s inventory rows are `PetSummaryDto` in `TraderSnapshotDto.pets`.

## Owned pet instance

| Concept (spec) | Source | Fields |
|----------------|--------|--------|
| Inventory row | `PetSummaryDto` | `id`, `breedName`, `ageYears`, `health`, `currentDesirability`, `intrinsicValue`, `maintenanceCost`, `isExpired` |

**Rules**: One UI entry per DTO in `pets[]` (FR-005). Optional aggregate copy may supplement list, not replace it.

## Activity item

| Concept (spec) | Source | Fields |
|----------------|--------|--------|
| Notification | `NotificationDto` | `id`, `type`, `createdAt`, `petId`, `petName`, `amount`, `counterpartyTraderId`, `counterpartyDisplayName` |

**Ordering**: Sort by `createdAt` descending if API does not guarantee order (verify backend contract; UI should display chronological recency as spec requires).

## State transitions (UI-only)

- **Workspace load**: `MyPetTraderProvider` loads snapshot; workspace may show loading until `traderId` resolved.
- **Realtime invalidation**: `useTradingPetsRealtime` bumps `reloadToken`; panels refetch breeds, listings, notifications; `refresh()` updates snapshot.
- **User actions**: Purchase, listing, bid paths call APIs then `onPurchased` / `onChanged` / `refresh`—no optimistic substitution of authoritative balances if it could mislead (PRF-003).
