---

description: "Task list for 008-split-trading-marketplaces"
---

# Tasks: Split trading into Primary supply and Resale marketplace

**Input**: Design documents from `/specs/008-split-trading-marketplaces/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included per `research.md` §7 and implementation plan testing gate — Vitest + Testing Library in `apps/frontend-spa` (`npm test`, `npm run lint`).

**Organization**: Tasks are grouped by user story (spec priorities) so each increment is independently verifiable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks in the same wave)
- **[Story]**: User story label `[US1]`, `[US2]`, `[US3]` from `spec.md`
- Every description includes at least one concrete file path

## Path Conventions

All implementation paths under `apps/frontend-spa/` per `plan.md` (SPA-only feature; market service unchanged).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Baseline toolchain before edits.

- [x] T001 Run `npm run lint` and `npm test` in `apps/frontend-spa/` to establish a clean baseline per `specs/008-split-trading-marketplaces/quickstart.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Pure listing partition used by resale layout (FR-004); must exist before User Story 3 implementation.

**⚠️ CRITICAL**: User Story 3 MUST NOT implement ad-hoc filters that diverge from this helper.

- [x] T002 Implement `partitionResaleListings` (or equivalent) for `MarketListingDto[]` by `sellerTraderId` vs session `traderId` in `apps/frontend-spa/src/features/trading-pets/marketListingsPartition.ts` per `specs/008-split-trading-marketplaces/data-model.md`
- [x] T003 [P] Add Vitest unit tests for `marketListingsPartition.ts` in `apps/frontend-spa/src/features/trading-pets/__tests__/marketListingsPartition.test.ts`

**Checkpoint**: Partition logic is tested and ready for resale UI.

---

## Phase 3: User Story 1 — Find and open the right market (Priority: P1) 🎯 MVP

**Goal**: Distinct routes, navigation labels, and page titles so **Primary supply market** vs **Resale marketplace** are unambiguous (FR-001, FR-005; `contracts/spa-routing.md`).

**Independent Test**: Signed-in user can reach each page via nav and see the correct `<h1>`; legacy `/pets/workspace` resolves to primary supply.

- [x] T004 [P] [US1] Create `PrimarySupplyMarketPage.tsx` in `apps/frontend-spa/src/features/trading-pets/` with accessible `<h1>Primary supply market</h1>` and minimal placeholder body
- [x] T005 [P] [US1] Create `ResaleMarketplacePage.tsx` in `apps/frontend-spa/src/features/trading-pets/` with accessible `<h1>Resale marketplace</h1>` and minimal placeholder body
- [x] T006 [US1] Export `PrimarySupplyMarketPage` and `ResaleMarketplacePage` from `apps/frontend-spa/src/features/trading-pets/index.ts`
- [x] T007 [US1] Register routes `/pets/primary-supply` and `/pets/resale` under the existing `MyPetTraderProvider` branch in `apps/frontend-spa/src/App.tsx` per `specs/008-split-trading-marketplaces/contracts/spa-routing.md`
- [x] T008 [US1] Add legacy `/pets/workspace` → `/pets/primary-supply` redirect (`replace`) and update `RootIndex` plus catch-all `*` route targets to `/pets/primary-supply` in `apps/frontend-spa/src/App.tsx`
- [x] T009 [US1] Replace the single “Pet trading” `NavLink` with two links (“Primary supply market”, “Resale marketplace”) pointing at the new paths in `apps/frontend-spa/src/App.tsx` (`Header`)
- [x] T010 [P] [US1] Add RTL coverage for dual nav links and `/pets/workspace` redirect in `apps/frontend-spa/src/features/trading-pets/__tests__/TradingMarketRoutes008.test.tsx`

**Checkpoint**: Navigation-only acceptance for US1 passes; pages may still be functionally incomplete.

---

## Phase 4: User Story 2 — Complete primary supply activities (Priority: P2)

**Goal**: Primary acquisition flows live only on **Primary supply market**; former “pet trading” expectation maps to primary supply (FR-002, US2).

**Independent Test**: Browse/purchase from primary supply works without visiting resale; copy points “pet trading” users to primary supply.

- [x] T011 [US2] Replace placeholder content in `apps/frontend-spa/src/features/trading-pets/PrimarySupplyMarketPage.tsx` with the trader refresh banner pattern from `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx` and embed `PrimaryMarketPanel` only per `specs/008-split-trading-marketplaces/research.md` §4
- [x] T012 [P] [US2] Update “Pet trading” links and copy to target `/pets/primary-supply` with FR-002-consistent wording in `apps/frontend-spa/src/features/trading-pets/MyPetsPage.tsx`
- [x] T013 [P] [US2] Update resale onboarding copy and link target that reference Pet trading in `apps/frontend-spa/src/features/trading-pets/MarketListingsPage.tsx`
- [x] T014 [US2] Remove unused combined workspace route usage: delete or slim `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx` after logic migration, ensuring no dangling imports in `apps/frontend-spa/src/App.tsx`
- [x] T015 [P] [US2] Migrate primary workspace tests from `apps/frontend-spa/src/features/trading-pets/__tests__/TraderWorkspacePage.test.tsx` to `apps/frontend-spa/src/features/trading-pets/__tests__/PrimarySupplyMarketPage.test.tsx` (or equivalent) asserting `PrimaryMarketPanel` behavior and heading

**Checkpoint**: US2 journeys work on `PrimarySupplyMarketPage` alone.

---

## Phase 5: User Story 3 — Manage and compare resale activity in two columns (Priority: P3)

**Goal**: **Resale marketplace** shows your listings vs others’ offers in two regions, responsive stack, strict separation (FR-003, FR-004, FR-006, FR-007; `contracts/spa-routing.md` accessibility).

**Independent Test**: With mocked listings, own sales never appear in the others’ column; empty/error states per column; narrow viewport stacks with labels preserved.

- [x] T016 [US3] Extract reusable resale listing UI pieces (list rows, seller actions integration points) from `apps/frontend-spa/src/features/trading-pets/SecondaryMarketPanel.tsx` into colocated components under `apps/frontend-spa/src/features/trading-pets/` for reuse by the resale page
- [x] T017 [US3] Implement full `ResaleMarketplacePage` in `apps/frontend-spa/src/features/trading-pets/ResaleMarketplacePage.tsx`: full-width “Offer a pet for sale”, two columns fed by `getMarketListings` once + `marketListingsPartition.ts`, full-width bid workflow below columns, `role="region"` + `aria-labelledby` for each column per `specs/008-split-trading-marketplaces/contracts/spa-routing.md`
- [x] T018 [US3] Add responsive two-column / stacked layout styles for resale regions in `apps/frontend-spa/src/styles.css` consistent with existing `trading-pets-*` variables per `specs/008-split-trading-marketplaces/research.md` §3
- [x] T019 [US3] Implement per-column loading/empty/error messaging and partial-failure behavior (one column usable if the other fails) in `apps/frontend-spa/src/features/trading-pets/ResaleMarketplacePage.tsx` per FR-006
- [x] T020 [US3] Ensure bid selection and bid actions only apply to others’ listings (`sellerTraderId !== traderId`) in `apps/frontend-spa/src/features/trading-pets/ResaleMarketplacePage.tsx`
- [x] T021 [P] [US3] Add RTL tests for column labels, partition behavior, and empty states in `apps/frontend-spa/src/features/trading-pets/__tests__/ResaleMarketplacePage.test.tsx`
- [x] T022 [P] [US3] Update `apps/frontend-spa/src/features/trading-pets/__tests__/SecondaryMarketPanel.test.tsx` to match refactored `SecondaryMarketPanel.tsx` or delete coverage if the panel is removed

**Checkpoint**: US3 acceptance scenarios hold on `/pets/resale` alone.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Quality gates, manual validation, consistency.

- [x] T023 [P] Run `npm run lint` and `npm test` in `apps/frontend-spa/` and fix any regressions introduced by the feature
- [ ] T024 [P] Execute manual verification steps in `specs/008-split-trading-marketplaces/quickstart.md` (including PRF-001 timing notes and SC-006 pass-rate recording in the PR description)

---

## Dependencies & Execution Order

### Phase Dependencies

```text
Phase 1 (Setup)
    → Phase 2 (Foundational partition)
        → Phase 3 (US1 routes/nav/pages)
            → Phase 4 (US2 primary page content)
                → Phase 5 (US3 resale layout) — depends on T002–T003 for correct partitioning
                    → Phase 6 (Polish)
```

### User Story Dependencies

| Story | Depends on | Notes |
|-------|------------|--------|
| **US1 (P1)** | Foundational optional for placeholder pages; partition not required for titles/nav | MVP when US1 + minimal placeholders done |
| **US2 (P2)** | US1 routes and `PrimarySupplyMarketPage.tsx` shell | Fills primary content |
| **US3 (P3)** | US1 resale route shell + **T002–T003** partition | Can start extraction (T016) in parallel with late US2 polish only if careful — partition tests should be green first |

### Parallel Opportunities

- **Phase 2**: T003 parallel with documentation-only work (not listed); after T002 completes, T003 is the test file.
- **Phase 3**: T004 ∥ T005; T010 after T006–T009.
- **Phase 4**: T012 ∥ T013; T015 after T011.
- **Phase 5**: T021 ∥ T022 after T017–T020.
- **Phase 6**: T023 ∥ T024 (different activities: automated vs manual).

---

## Parallel Example: User Story 1

```bash
# After T006 is decided (exports), developers can split:
Task T004: PrimarySupplyMarketPage.tsx (skeleton)
Task T005: ResaleMarketplacePage.tsx (skeleton)

# Tests after routes wired:
Task T010: TradingMarketRoutes008.test.tsx
```

---

## Parallel Example: User Story 3

```bash
# After resale page implementation stabilizes:
Task T021: ResaleMarketplacePage.test.tsx
Task T022: SecondaryMarketPanel.test.tsx (update)
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1–2 (baseline + partition helper for forward compatibility).
2. Complete Phase 3 (US1): routes, redirects, nav, titled shells, routing tests.
3. **STOP and VALIDATE**: US1 independent test (nav + titles + redirect).

### Incremental Delivery

1. Add Phase 4 (US2): primary supply fully functional on dedicated page.
2. Add Phase 5 (US3): two-column resale + tests.
3. Phase 6: lint/test + `quickstart.md` evidence.

### Parallel Team Strategy

- Developer A: US1/US2 (`App.tsx`, `PrimarySupplyMarketPage.tsx`, copy updates).
- Developer B: Foundational partition + US3 resale layout + styles (after US1 shells merged).

---

## Notes

- Do not change market-service HTTP contracts (`specs/008-split-trading-marketplaces/contracts/http-market-api.md`).
- `tradingPetsApi.ts` should remain unchanged unless integration proves impossible (plan default: unchanged).
- Realtime behavior stays on `MyPetTraderProvider` / `useTradingPetsRealtime` per `research.md` §5.
