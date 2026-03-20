---

description: "Task list for Trading Pets Platform (004-trading-pets)"
---

# Tasks: Trading Pets Platform

**Input**: Design documents from `C:\work\my\specit_trading_test\specs\004-trading-pets\` and authoritative domain detail in `C:\work\my\specit_trading_test\system_reqs.md`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included per `plan.md` constitution (xUnit + contract tests + key Vitest/RTL coverage) and `specs/004-trading-pets/contracts/api-contracts.md` §Contract tests mapping.

**Organization**: Phases follow user stories P1–P4 from `spec.md` for independent delivery and verification.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no ordering dependency on incomplete tasks)
- **[Story]**: `[US1]`–`[US4]` for user-story phases only
- Every description ends with an exact file path

## Path Conventions (this repo)

- Backend: `apps/services/market-service/src/`
- Frontend: `apps/frontend-spa/src/`
- Contract tests: `tests/contract/market-service/MarketService.ContractTests/`
- Unit tests: `tests/unit/market-service/MarketService.UnitTests/`
- Harness: `libs/test-support/src/Trading.TestSupport/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Configuration documentation and frontend feature scaffold so later tasks have stable entry points.

- [x] T001 Document Trading Pets settings keys (`InitialTraderCash`, `DefaultSupplyPerBreed`, `ValuationTickSeconds`, demo trader allowlist) in `apps/services/market-service/src/MarketService/appsettings.json`, set initially to be 1000$
- [x] T002 [P] Add trading-pets feature scaffold (`index.ts` exports placeholder) in `apps/frontend-spa/src/features/trading-pets/index.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Schema, seed data, valuation rules, authorization, and host registration required before any user story is complete end-to-end.

**⚠️ CRITICAL**: No user story phase is shippable until this phase completes (migrations applied; traders/breeds/supply exist; formula and tick behavior testable).

- [x] T003 [P] Add domain entities `Breed`, `Supply`, `Pet`, `Listing`, `Bid`, `Trade`, `Notification`, `Trader` per `specs/004-trading-pets/data-model.md` in `apps/services/market-service/src/MarketService.Domain/Entities/`
- [x] T004 Configure EF mappings, relationships, and indexes (active listing per pet, notification ordering) in `apps/services/market-service/src/MarketService.Infrastructure/Persistence/MarketDbContext.cs`
- [x] T005 Add EF Core migration and apply/update database for trading-pets tables in `apps/services/market-service/src/MarketService.Infrastructure/Persistence/Migrations/`
- [x] T006 [P] Seed exactly twenty breeds and per-breed `Supply` from `system_reqs.md` §4 in `apps/services/market-service/src/MarketService.Infrastructure/Persistence/MarketPetSeedData.cs`
- [x] T007 Seed configurable `Trader` rows and initial cash per `specs/004-trading-pets/research.md` R-6 in `apps/services/market-service/src/MarketService.Infrastructure/Seeding/SeedDataRunner.cs`
- [x] T008 Implement intrinsic value calculation (FR-007, R-3, R-8 rounding) in `apps/services/market-service/src/MarketService.Application/Pets/PetIntrinsicValueCalculator.cs`
- [x] T009 [P] Implement valuation tick worker (FR-008, R-4 variance + clamps) as `IHostedService` in `apps/services/market-service/src/MarketService.Application/Pets/PetValuationTickHostedService.cs`
- [x] T010 [P] Add unit tests for intrinsic value, lifespan clamp, and tick variance bounds in `tests/unit/market-service/MarketService.UnitTests/PetValuationTests.cs`
- [x] T011 Implement server-side checks that command `traderId` is allowed for the current user (R-6) in `apps/services/market-service/src/MarketService.Application/Authorization/TraderAuthorizationHelper.cs`
- [x] T012 Register new endpoints services, hosted valuation tick, and DbContext updates in `apps/services/market-service/src/MarketService/Program.cs`

**Checkpoint**: Database has 20 breeds + supply + N traders; formula unit tests pass; unauthorized cross-trader commands fail.

---

## Phase 3: User Story 1 — Buy pets and manage a trader’s holdings (Priority: P1) 🎯 MVP

**Goal**: Primary purchase from limited supply; per-trader snapshot (available cash, locked cash, inventory, portfolio); no cross-trader leakage (FR-001–FR-006, FR-009, SC-003).

**Independent Test**: Configure ≥2 traders; as Trader A purchase until cash or supply blocks; Trader A snapshot shows pets and reduced cash; Trader B snapshot unchanged; blocked purchase returns clear error (UX-002).

### Tests for User Story 1

- [x] T013 [P] [US1] Contract test `GET /api/pets/breeds` shape and `remainingSupply` in `tests/contract/market-service/MarketService.ContractTests/PetsBreedsContractTests.cs`
- [x] T014 [P] [US1] Contract test `POST /api/pets/purchase` success, insufficient cash, insufficient supply in `tests/contract/market-service/MarketService.ContractTests/PetsPurchaseContractTests.cs`
- [x] T015 [P] [US1] Contract test `GET /api/traders/{traderId}/snapshot` isolation between traders in `tests/contract/market-service/MarketService.ContractTests/TradersSnapshotContractTests.cs`

### Implementation for User Story 1

- [x] T016 [P] [US1] Implement `GET /api/pets/breeds` per `specs/004-trading-pets/contracts/api-contracts.md` in `apps/services/market-service/src/MarketService.Api/Endpoints/PetsBreedsEndpoints.cs`
- [x] T017 [US1] Implement `POST /api/pets/purchase` (R-7 batch) and handler in `apps/services/market-service/src/MarketService.Application/Pets/PurchasePetsHandler.cs`
- [x] T018 [US1] Implement `GET /api/traders/{traderId}/snapshot` (portfolio + pets + empty `myBids` placeholder) in `apps/services/market-service/src/MarketService.Api/Endpoints/TradersSnapshotEndpoints.cs`
- [x] T019 [US1] Implement EF persistence for purchase and snapshot queries in `apps/services/market-service/src/MarketService.Infrastructure/Persistence/MarketPetDataStore.cs`
- [x] T020 [US1] Build trader workspace (active trader switcher, available/locked cash, portfolio, inventory) in `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx`
- [x] T021 [US1] Build primary market panel (breed list, quantity, purchase) in `apps/frontend-spa/src/features/trading-pets/PrimaryMarketPanel.tsx`
- [x] T022 [US1] Register trading-pets routes and navigation in `apps/frontend-spa/src/App.tsx`

**Checkpoint**: MVP demo: buy pets, see private panel, verify other trader unchanged.

---

## Phase 4: User Story 2 — Trade pets on the secondary market (Priority: P2)

**Goal**: List/withdraw; crossing bid immediate trade (FR-014); single highest below-ask bid with lock/release; outbid; buyer withdraw; seller accept/reject; listing withdraw with bid cancellation (FR-010–FR-017, FR-021 partial via persistence).

**Independent Test**: Two traders: (a) list → bid ≥ ask → immediate trade and balances; (b) below-ask → higher below-ask outbid → seller accept; verify locks, transfer, and errors for self-bid.

**Depends on**: User Story 1 (owned pets, cash, snapshot).

### Tests for User Story 2

- [x] T023 [P] [US2] Contract tests `POST /api/market/listings`, withdraw, duplicate listing, not owner in `tests/contract/market-service/MarketService.ContractTests/MarketListingsContractTests.cs`
- [x] T024 [P] [US2] Contract tests bids: cross-trade, below-ask active, outbid, withdraw bid, accept, reject, listing withdraw with pending bid in `tests/contract/market-service/MarketService.ContractTests/MarketBidsContractTests.cs`

### Implementation for User Story 2

- [x] T025 [US2] Implement secondary-market command handlers (list, withdraw listing, place bid, withdraw bid, accept, reject) in `apps/services/market-service/src/MarketService.Application/Market/SecondaryMarketHandlers.cs`
- [x] T026 [US2] Map Minimal API routes for listings and bids per `specs/004-trading-pets/contracts/api-contracts.md` in `apps/services/market-service/src/MarketService.Api/Endpoints/MarketTradingEndpoints.cs`
- [x] T027 [US2] Persist listing/bid/trade state, cash locks, and `Trade` rows; enforce one active listing and one pending below-ask bid in `apps/services/market-service/src/MarketService.Infrastructure/Persistence/MarketDbContext.cs`
- [x] T028 [P] [US2] Build secondary market UI (list pet, bid entry, crossing vs pending states, FR-016 buyer visibility) in `apps/frontend-spa/src/features/trading-pets/SecondaryMarketPanel.tsx`
- [x] T029 [US2] Build seller actions and blocked-action messages (UX-002) in `apps/frontend-spa/src/features/trading-pets/ListingSellerActions.tsx`

**Checkpoint**: Full §5.2, §5.3, §5.5, §5.6 flows from `specs/004-trading-pets/quickstart.md` achievable via API + UI.

---

## Phase 5: User Story 3 — Understand the market and compare traders (Priority: P3)

**Goal**: Market feed (newest first, ask, recent trade price rule R-2, supply counts); analysis fundamentals; leaderboard by portfolio (FR-018–FR-020).

**Independent Test**: Seed known listings/trades; verify sort and `recentTradePriceForBreed`; open analysis for visible pet; leaderboard order matches computed totals.

**Depends on**: User Story 1 (supply, pets); User Story 2 (listings/trades for full market context).

### Tests for User Story 3

- [x] T030 [P] [US3] Contract test `GET /api/market/listings` fields and default sort in `tests/contract/market-service/MarketService.ContractTests/MarketListingsFeedContractTests.cs`
- [x] T031 [P] [US3] Contract tests `GET /api/pets/{petId}/analysis` and `GET /api/traders/leaderboard` in `tests/contract/market-service/MarketService.ContractTests/PetsAnalysisAndLeaderboardContractTests.cs`

### Implementation for User Story 3

- [x] T032 [US3] Ensure `GET /api/market/listings` includes `recentTradePriceForBreed` (R-2) and `remainingNewSupplyForBreed` in `apps/services/market-service/src/MarketService.Api/Endpoints/MarketTradingEndpoints.cs`
- [x] T033 [US3] Implement `GET /api/pets/{petId}/analysis` (visibility: listed or owner) in `apps/services/market-service/src/MarketService.Api/Endpoints/PetsAnalysisEndpoints.cs`
- [x] T034 [US3] Implement `GET /api/traders/leaderboard` sorted by `portfolioTotal` in `apps/services/market-service/src/MarketService.Api/Endpoints/TradersLeaderboardEndpoints.cs`
- [x] T035 [P] [US3] Build market listings page in `apps/frontend-spa/src/features/trading-pets/MarketListingsPage.tsx`
- [x] T036 [P] [US3] Build pet analysis page in `apps/frontend-spa/src/features/trading-pets/PetAnalysisPage.tsx`
- [x] T037 [US3] Build leaderboard page in `apps/frontend-spa/src/features/trading-pets/LeaderboardPage.tsx`
- [x] T038 [US3] Register market, analysis, and leaderboard routes in `apps/frontend-spa/src/App.tsx`

**Checkpoint**: §5.7–5.8 quickstart flows satisfied with UI.

---

## Phase 6: User Story 4 — Stay informed as values and deals change (Priority: P4)

**Goal**: Notification feed (FR-021 types, chronological, R-9 ordering); SignalR pushes + documented poll fallback (R-5, PRF-001, PRF-003); panels refresh after trades and valuation tick.

**Independent Test**: Script each notification type; disconnect hub and confirm polling updates trader snapshot and notifications within fallback interval; after tick, intrinsic/portfolio visible without full reload.

**Depends on**: User Stories 1–3 (events and reads exist).

### Tests for User Story 4

- [x] T039 [P] [US4] Contract test `GET /api/traders/{traderId}/notifications` types and ordering in `tests/contract/market-service/MarketService.ContractTests/NotificationsContractTests.cs`

### Implementation for User Story 4

- [x] T040 [US4] Implement `GET /api/traders/{traderId}/notifications` in `apps/services/market-service/src/MarketService.Api/Endpoints/TradersNotificationsEndpoints.cs`
- [x] T041 [US4] Extend hub: `trader.snapshotUpdated`, `market.listingsUpdated`, `trader.notificationsAdded`, `leaderboard.updated`, `pet.valuationBatch` per `specs/004-trading-pets/contracts/realtime-contracts.md` in `apps/services/market-service/src/MarketService.Api/Hubs/MarketHub.cs`
- [x] T042 [US4] Publish hub events after commands and valuation tick from `apps/services/market-service/src/MarketService.Application/Realtime/MarketRealtimeNotifier.cs`
- [x] T043 [P] [US4] Build notification feed UI (consistent labels UX-001) in `apps/frontend-spa/src/features/trading-pets/NotificationsPanel.tsx`
- [x] T044 [US4] Implement SignalR subscribe + `>10s` disconnect → `5s` poll per `specs/004-trading-pets/quickstart.md` in `apps/frontend-spa/src/features/trading-pets/useTradingPetsRealtime.ts`
- [x] T045 [P] [US4] Add Vitest tests with mocked hub/poll in `apps/frontend-spa/src/features/trading-pets/__tests__/tradingPetsRealtime.test.tsx`

**Checkpoint**: SC-004, SC-005 rehearsal possible; PRF-003 fallback documented in UI behavior.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: UX consistency, harness support, contract doc accuracy, quickstart validation.

- [x] T046 [P] Align labels for available vs locked cash and bid/listing states across `apps/frontend-spa/src/features/trading-pets/`
- [x] T047 [P] Extend harness helpers for pet APIs and auth headers in `libs/test-support/src/Trading.TestSupport/TradingPlatformHarness.cs`
- [x] T048 Document final SignalR hub path and method names in `specs/004-trading-pets/contracts/realtime-contracts.md`
- [ ] T049 Run timed checks from `specs/004-trading-pets/quickstart.md` and record PRF-001 results in `specs/004-trading-pets/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1** → **Phase 2** → **User Story phases** → **Phase 7**
- **User Story 2** requires **User Story 1** (pets, cash, breeds API).
- **User Story 3** requires **User Story 1** and **User Story 2** for full acceptance (listings/trades + analysis visibility rules).
- **User Story 4** requires **User Stories 1–3** so all event types and reads exist.

### User Story Dependency Graph

```text
US1 (P1) ──► US2 (P2) ──► US3 (P3)
                │              │
                └──────┬───────┘
                       ▼
                  US4 (P4)
```

### Within Each User Story

- Contract tests (T013–T015, etc.) before or alongside implementation; they must fail until endpoints exist.
- Application handlers before or with API mapping; persistence backing before declaring API “done”.
- Frontend after corresponding API contracts are stable.

### Parallel Opportunities

- **Phase 2**: T003, T006, T009, T010 can proceed in parallel once entity shapes are agreed; T004–T005 serialization after entity files exist.
- **US1**: T013–T015 parallel; T016–T017 parallel after T019 store interface is sketched; T020–T021 parallel after snapshot DTO is fixed.
- **US2**: T023–T024 parallel; T028 parallel with backend tasks once DTOs are known.
- **US3**: T030–T031 parallel; T035–T036 parallel.
- **US4**: T039 parallel with T041 design; T043–T045 parallel after hook contract is stable.
- **Phase 7**: T046–T047 parallel.

---

## Parallel Example: User Story 2

```text
# Start contract tests together:
Task T023 → MarketListingsContractTests.cs
Task T024 → MarketBidsContractTests.cs

# After handlers exist, UI can parallelize:
Task T028 → SecondaryMarketPanel.tsx
(producer) Task T025–T027 on backend
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1–2 (schema, seed, formula, tick, auth).
2. Complete Phase 3 (US1) including contract tests.
3. **STOP**: Run US1 independent test (two traders, privacy, purchase limits).

### Incremental Delivery

1. US1 → demo primary market + panels  
2. US2 → secondary trading differentiator  
3. US3 → market intelligence + leaderboard  
4. US4 → realtime narrative + fallback  
5. Phase 7 → facilitator polish and timing evidence  

### Parallel Team Strategy

- After Phase 2: Developer A (US1 frontend), B (US1 API), C (contract tests) until API stabilizes; then pipeline US2 similarly.

---

## Notes

- **Configurable N traders** (FR-001): never hard-code three traders; default `3` in `system_reqs.md` §2.2 is **supply per breed**, not trader count (A-005).
- **Recent trade price** (R-2): last completed **secondary** trade **for the pet’s breed**, or null — not intrinsic value.
- **Money/intrinsic**: `decimal(18,2)` and 2dp display for SC-002 (R-8).
