# Research: Split trading into Primary supply and Resale marketplace (008)

Consolidated decisions for implementation. Sources: `spec.md`, constitution, and current `apps/frontend-spa` trading feature (`TraderWorkspacePage`, `PrimaryMarketPanel`, `SecondaryMarketPanel`, `App.tsx`).

## 1. Scope and backend surface

**Decision**: Treat this feature as **frontend information architecture and layout only**. Keep **existing** market HTTP endpoints and DTOs (`tradingPetsApi.ts`); do **not** add a server endpoint solely to split “my listings” vs “others’” unless profiling shows the combined list is unusable at scale.

**Rationale**: Spec A-001/A-002 state underlying rules and resale concept are unchanged; `MarketListingDto.sellerTraderId` plus session `traderId` enable strict client-side partitioning (FR-004).

**Alternatives considered**: `GET /api/market/listings/mine` and `/others` (rejected for initial delivery: extra contract surface and cache invalidation complexity). Single endpoint with `?scope=` query (rejected: same as above with less clarity).

## 2. Routing and navigation

**Decision**: Expose two **distinct** React Router destinations:

| Route (proposed) | Page role |
|------------------|-----------|
| `/pets/primary-supply` | **Primary supply market** — content currently in `PrimaryMarketPanel` (and the same workspace-level refresh banner pattern as today). |
| `/pets/resale` | **Resale marketplace** — resale flows split into two listing columns plus supporting controls (see §3). |

Replace the single header link **“Pet trading”** (`/pets/workspace`) with **two** `NavLink`s labeled per spec (e.g. **Primary supply market**, **Resale marketplace**), satisfying FR-001 and FR-005.

**Legacy URL**: Keep **`/pets/workspace`** as a **`Navigate` redirect** to `/pets/primary-supply` (or remove the old route after one release — redirect preferred for SC-004).

Update the catch-all route (`path="*"`) default from `/pets/workspace` to **`/pets/primary-supply`** so unauthenticated or unknown paths land on the primary-supply experience consistently with the renamed “pet trading” home.

**Rationale**: FR-001 requires distinct destinations; redirect preserves bookmarks and User Story 2 expectation that former “pet trading” maps to primary supply (FR-002).

**Alternatives considered**: Tabs on one URL (rejected: not separate pages per FR-001). Keeping `/pets/workspace` as the primary URL without rename (rejected: weak alignment with “Primary supply market” wording).

## 3. Resale page layout and behavior

**Decision**:

1. **Offer a pet for sale** (inventory select + ask + post) remains on **Resale marketplace** only — place it **above** the two-column region (full width) so sellers always see create-listing affordance regardless of column scroll state.
2. **Two columns** (desktop/tablet):  
   - **Column A — Your listings**: `listings.filter(l => l.sellerTraderId === traderId)`; empty state when none (FR-006).  
   - **Column B — Others’ offers**: `listings.filter(l => l.sellerTraderId !== traderId)`; strict separation (FR-004).  
3. **Place bid** (and related selection UX): implement **once** below the columns (full width), operating on a listing selected from **column B only**; do not allow selecting own listings for bidding (matches current mental model).
4. **Narrow viewports**: stack **Your listings** then **Others’ offers** (or vice versa — prefer “yours” first per seller mental model), preserving headings (spec edge case).

Reuse existing child components where possible (`ListingSellerActions`, `listingSellerClause`, listing card markup) to satisfy UX-001.

**Rationale**: Maximizes “real estate” for scanning two lists (User Story 3) without duplicating bid logic; create-listing stays discoverable.

**Alternatives considered**: Duplicate bid blocks per column (rejected: error-prone). Putting “offer for sale” only in the left column (acceptable variant; full-width top chosen for visibility when left column is long).

## 4. Primary supply page content

**Decision**: **Primary supply market** page includes the **page-level** workspace header pattern (title **Primary supply market**, intro copy focused on primary acquisition, link to **My pets** as today) and **`PrimaryMarketPanel` only**. Omit secondary/resale panel and resale-specific instructions from this page.

**Rationale**: FR-002 isolates primary-supply wording and journeys; FR-003 places resale-only UX on the resale route.

**Alternatives considered**: Minimal title-only wrapper (rejected: loses helpful intro and balance refresh context already expected on workspace).

## 5. Realtime refresh and context

**Decision**: Keep **`MyPetTraderProvider`** wrapping both new routes (same as current `/pets/workspace` subtree) so `traderId`, `snapshot`, `refresh`, and `hubInvalidateSeq` behave identically. **`PrimaryMarketPanel`** continues `onPurchased` → `refresh`; resale page continues `onChanged` → `refresh` and listing reload on `reloadToken` changes.

**Rationale**: Reuses proven SignalR invalidation (`useTradingPetsRealtime`); no new cross-page state library.

**Alternatives considered**: Separate providers per page (rejected: redundant hub connections).

## 6. Auth and anonymous behavior

**Decision**: Follow **existing** `ProtectedRoute` rules: if trading routes remain protected, signed-out users never see the split (consistent with current app). If any trading view is reachable signed-out in the future, apply the same view-only / prompt patterns as today — **no new auth model** in this feature.

**Rationale**: Spec edge case defers to current product rules.

## 7. Testing strategy

**Decision**: Add **Vitest + RTL** coverage for: (1) **partition helper** or pure filter ensuring no `sellerTraderId === traderId` row appears in the “others” list; (2) **Resale marketplace** renders two labeled regions and correct empty states; (3) **routing** — nav contains both destinations and legacy redirect works if implemented. Run `npm test` and `npm run lint` in CI.

**Rationale**: Constitution Principle II; aligns with 006 testing approach.

**Alternatives considered**: E2E-only (rejected as sole gate; optional addition).

## 8. Performance validation (PRF-001–PRF-003)

**Decision**: Add a **repeatable QA checklist** in `quickstart.md`: cold navigate to each route, note time to first meaningful content (skeleton or first heading + first loaded row) on a documented connection; repeat **N** runs and record pass rate for SC-006. If resale loads two column fetches in the future, document — **current design** uses a **single** `getMarketListings` call and client split (no extra latency vs today’s combined list).

**Rationale**: Constitution Principle IV; spec requires documented evidence or exception.

## 9. Dependencies and simplicity

**Decision**: **No new npm dependencies** for layout; use existing CSS grid/flex patterns (`trading-pets-*` classes) and extend stylesheet only as needed.

**Rationale**: Constitution Principle V.
