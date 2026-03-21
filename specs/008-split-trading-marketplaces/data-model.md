# Data model: Split Primary supply / Resale marketplace (client view)

This feature does **not** introduce new server tables or API fields. It maps **spec entities** to **existing DTOs** in `apps/frontend-spa/src/features/trading-pets/tradingPetsApi.ts` and defines **client-side display classification** for resale.

## Market session context

| Concept (spec) | Source | Fields / rule |
|----------------|--------|----------------|
| Signed-in trader | `TraderSnapshotDto` | `traderId` from `GET /api/traders/me/snapshot` (via `MyPetTraderContext`) |
| Anonymous / signed-out | App routing | Same as current product: typically no access to protected trading routes |

**Validation**: “Mine” vs “others’” for resale listings uses **`traderId === listing.sellerTraderId`** (FR-004).

## Primary supply offer (per breed)

| Concept (spec) | Source | Fields |
|----------------|--------|--------|
| Breed catalog | `BreedDto[]` | `id`, `name`, `retailPrice`, `remainingSupply`, … |
| Purchase | `POST /api/pets/purchase` | `traderId`, `breedId`, `quantity` |

**UI location**: **Primary supply market** page only (`PrimaryMarketPanel`).

## Resale listing (unified feed, split in UI)

| Concept (spec) | Source | Partition rule |
|----------------|--------|----------------|
| Resale listing | `MarketListingDto` | `listingId`, `petId`, `sellerTraderId`, `breedName`, `askingPrice`, `sellerDisplayName`, `sellerEmail?`, … |
| **Your listings** (mine) | Same array from `GET /api/market/listings` | `sellerTraderId === currentTraderId` |
| **Others’ offers** | Same array | `sellerTraderId !== currentTraderId` |

**Actions** (unchanged endpoints):

- Create: `POST /api/market/listings`
- Bid: `POST /api/market/listings/{listingId}/bids` (targets others’ listings only in UX)
- Withdraw listing / bid, accept/reject: existing helpers in `tradingPetsApi.ts`

**Relationships**: Inventory for “offer for sale” remains `TraderSnapshotDto.pets` (`PetSummaryDto[]`).

## Inventory pet instance

| Concept (spec) | Source | Fields |
|----------------|--------|--------|
| Pet in inventory | `PetSummaryDto` | `id`, `breedName`, … |

Used on **Resale marketplace** for the create-listing form.

## State transitions (UI)

- **Load**: `getMarketListings` returns full open listing set; UI derives two columns without a second request.
- **Invalidate**: `hubInvalidateSeq` / `market.listingsUpdated` → reload listings; `trader.snapshotUpdated` → parent `refresh()` updates inventory and cash.
- **Consistency**: After create/withdraw/bid, refetch listings and/or snapshot same as `SecondaryMarketPanel` today.

## Validation rules (requirements traceability)

| Rule | Enforcement |
|------|-------------|
| FR-004 | Filter predicates are mutually exclusive and cover all listings for a given trader id. |
| FR-006 | Per-column loading, empty, and error messaging; if `getMarketListings` fails, both columns show the shared error (or one banner + disabled lists). |
