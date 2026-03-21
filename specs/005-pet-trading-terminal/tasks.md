---

description: "Task list for Pet Trading Terminal Experience (005-pet-trading-terminal)"
---

# Tasks: Pet Trading Terminal Experience

**Input**: Design documents from `C:\work\my\specit_trading_test\specs\005-pet-trading-terminal\`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included per `plan.md` constitution and `spec.md` testing mandate: xUnit unit + contract + integration coverage for `market-service`, plus Vitest + Testing Library coverage for the SPA terminal route, realtime behavior, and diff-based highlighting.

**Organization**: Phases follow user stories P1-P3 from `spec.md` so each increment can be implemented and verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel when it touches a different file and does not depend on incomplete work
- **[Story]**: `[US1]`, `[US2]`, `[US3]` only for user-story phases
- Every task ends with an exact file path

## Path Conventions (this repo)

- Backend: `apps/services/market-service/src/`
- Frontend: `apps/frontend-spa/src/`
- Contract tests: `tests/contract/market-service/MarketService.ContractTests/`
- Integration tests: `tests/integration/`
- Unit tests: `tests/unit/market-service/MarketService.UnitTests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create stable terminal-specific entry points before implementation starts.

- [X] T001 Create the frontend terminal barrel export and feature scaffold in `apps/frontend-spa/src/features/trading-pets/terminal/index.ts`
- [X] T002 [P] Create terminal DTO and result record definitions used across handlers and endpoints in `apps/services/market-service/src/MarketService.Application/Pets/Terminal/TerminalContracts.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared terminal abstractions and plumbing that all user stories depend on.

**⚠️ CRITICAL**: No user-story phase should be considered complete until these tasks are done.

- [X] T003 Extend the pet-trading store contract with terminal market, workspace, and order methods in `apps/services/market-service/src/MarketService.Application/Abstractions/IMarketPetStore.cs`
- [X] T004 [P] Add shared unit coverage for order-book aggregation, trend derivation, and deterministic quantity allocation in `tests/unit/market-service/MarketService.UnitTests/PetTradingTerminalProjectionTests.cs`
- [X] T005 Implement shared terminal projections and reusable query helpers over breeds, listings, bids, trades, and trader inventory in `apps/services/market-service/src/MarketService.Infrastructure/Persistence/MarketPetDataStore.cs`
- [X] T006 [P] Add a shared terminal workspace loading hook for markets, selected market state, and error retention in `apps/frontend-spa/src/features/trading-pets/terminal/useTradingTerminalWorkspace.ts`
- [X] T007 Register terminal handlers and endpoint groups in `apps/services/market-service/src/MarketService/Program.cs`

**Checkpoint**: Terminal read/write abstractions exist, projection rules are testable, and both apps have stable extension points for story work.

---

## Phase 3: User Story 1 - Trade from a single terminal workspace (Priority: P1) 🎯 MVP

**Goal**: Replace the current Pet Trading workspace with one terminal-style screen that shows market rows, a selected-market order book, trading context, and recent trades from backend state.

**Independent Test**: Open `/pets/workspace` with seeded market activity and verify the page alone shows the four terminal regions, selected-market details, account context, and explicit empty/error states without leaving the workspace.

### Tests for User Story 1

- [X] T008 [P] [US1] Add contract coverage for `GET /api/pets/terminal/markets` market rows and nullable pricing fields in `tests/contract/market-service/MarketService.ContractTests/PetsTerminalMarketsContractTests.cs`
- [X] T009 [P] [US1] Add contract coverage for `GET /api/pets/terminal/workspace/{marketEntryId}` order-book, account-summary, and recent-trades payloads in `tests/contract/market-service/MarketService.ContractTests/PetsTerminalWorkspaceContractTests.cs`
- [X] T010 [P] [US1] Add frontend render and market-selection coverage for the terminal workspace route in `apps/frontend-spa/src/features/trading-pets/__tests__/TraderWorkspacePage.test.tsx`

### Implementation for User Story 1

- [X] T011 [US1] Implement the terminal markets query handler and breed-level row shaping in `apps/services/market-service/src/MarketService.Application/Pets/Terminal/GetTerminalMarketsHandler.cs`
- [X] T012 [US1] Implement the selected-market workspace snapshot handler in `apps/services/market-service/src/MarketService.Application/Pets/Terminal/GetTerminalWorkspaceHandler.cs`
- [X] T013 [US1] Map the read-only terminal endpoints for markets and workspace snapshots in `apps/services/market-service/src/MarketService.Api/Endpoints/PetsTerminalEndpoints.cs`
- [X] T014 [P] [US1] Build the left-side market list with selection state and trend badges in `apps/frontend-spa/src/features/trading-pets/terminal/TerminalMarketList.tsx`
- [X] T015 [P] [US1] Build the order-book panel with explicit empty bid/ask states in `apps/frontend-spa/src/features/trading-pets/terminal/TerminalOrderBook.tsx`
- [X] T016 [P] [US1] Build the recent-trade feed panel with newest-first rows in `apps/frontend-spa/src/features/trading-pets/terminal/TerminalTradeFeed.tsx`
- [X] T017 [US1] Build the trading-panel shell that shows account summary, owned quantity, and selected-market context in `apps/frontend-spa/src/features/trading-pets/terminal/TerminalTradingPanel.tsx`
- [X] T018 [US1] Refactor the Pet Trading workspace route into the four-region terminal layout and selection flow in `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx`

**Checkpoint**: `/pets/workspace` feels like a single trading terminal backed by terminal read models, even before order submission behavior is wired.

---

## Phase 4: User Story 2 - Submit trading actions that stay backend-authoritative (Priority: P2)

**Goal**: Let traders place bids, asks, and buy-now requests from the terminal while every outcome remains determined by existing backend rules and immediately reconciled back into the workspace.

**Independent Test**: From `/pets/workspace`, submit one bid, one ask, and one buy-now request and verify the displayed result, order book, balance, owned pets, and recent trades all match the backend-confirmed outcome.

### Tests for User Story 2

- [X] T019 [P] [US2] Add contract coverage for terminal bid, ask, and buy-now endpoints in `tests/contract/market-service/MarketService.ContractTests/PetsTerminalOrdersContractTests.cs`
- [X] T020 [P] [US2] Add backend integration coverage for multi-step terminal order submission and workspace reconciliation in `tests/integration/market-trade/MarketTrade.IntegrationTests/PetTradingTerminalOrderFlowTests.cs`
- [X] T021 [P] [US2] Add frontend coverage for submit states, validation failures, and backend outcome messaging in `apps/frontend-spa/src/features/trading-pets/__tests__/TerminalTradingPanel.test.tsx`

### Implementation for User Story 2

- [X] T022 [US2] Implement backend multi-quantity ask, bid, and buy-now execution over existing pet-trading rules in `apps/services/market-service/src/MarketService.Infrastructure/Persistence/MarketPetDataStore.cs`
- [X] T023 [US2] Implement normalized terminal order orchestration and result-message shaping in `apps/services/market-service/src/MarketService.Application/Pets/Terminal/PlaceTerminalOrderHandler.cs`
- [X] T024 [US2] Map terminal order endpoints for `/orders/bid`, `/orders/ask`, and `/orders/buy-now` in `apps/services/market-service/src/MarketService.Api/Endpoints/PetsTerminalOrdersEndpoints.cs`
- [X] T025 [US2] Extend terminal order DTOs and REST clients for bid, ask, and buy-now flows in `apps/frontend-spa/src/features/trading-pets/tradingPetsApi.ts`
- [X] T026 [US2] Wire trading-panel submit actions, optimistic-disabled states, and backend-confirmed outcome banners in `apps/frontend-spa/src/features/trading-pets/terminal/TerminalTradingPanel.tsx`
- [X] T027 [US2] Reconcile post-submit refreshes and stale-state recovery at the workspace level in `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx`

**Checkpoint**: Traders can act directly from the terminal page and the page always converges back to the backend-confirmed state.

---

## Phase 5: User Story 3 - Monitor market movement in real time (Priority: P3)

**Goal**: Keep the terminal page feeling live with invalidation-driven refreshes, tighter polling fallback, and clear visual emphasis for new trades and price movement.

**Independent Test**: Leave `/pets/workspace` open while other trading actions occur and verify that market rows, order book, account summary, and trade feed refresh within the target window while new trades and price changes remain visually obvious.

### Tests for User Story 3

- [X] T028 [P] [US3] Add backend integration coverage for terminal-related realtime invalidation paths in `tests/integration/realtime/Realtime.IntegrationTests/PetTradingTerminalRealtimeTests.cs`
- [X] T029 [P] [US3] Expand terminal SignalR and polling-fallback coverage in `apps/frontend-spa/src/features/trading-pets/__tests__/tradingPetsRealtime.test.tsx`
- [X] T030 [P] [US3] Add diff/highlight coverage for new trades and changed prices in `apps/frontend-spa/src/features/trading-pets/__tests__/TerminalHighlights.test.tsx`

### Implementation for User Story 3

- [X] T031 [US3] Route terminal-related invalidation events through the existing realtime notifier flows in `apps/services/market-service/src/MarketService.Application/Realtime/MarketRealtimeNotifier.cs`
- [X] T032 [US3] Implement snapshot-diff helpers for new-trade and price-change highlighting in `apps/frontend-spa/src/features/trading-pets/terminal/terminalHighlights.ts`
- [X] T033 [US3] Tighten the terminal route fallback to roughly 3-second disconnect detection and 2-second polling in `apps/frontend-spa/src/features/trading-pets/useTradingPetsRealtime.ts`
- [X] T034 [US3] Apply new-trade highlighting and transient activity states in `apps/frontend-spa/src/features/trading-pets/terminal/TerminalTradeFeed.tsx`
- [X] T035 [US3] Apply price-direction flashes and trend emphasis in `apps/frontend-spa/src/features/trading-pets/terminal/TerminalMarketList.tsx`
- [X] T036 [US3] Apply bid/ask-side emphasis and refreshed-level highlighting in `apps/frontend-spa/src/features/trading-pets/terminal/TerminalOrderBook.tsx`

**Checkpoint**: The terminal continues to feel active under live market changes, and fallback behavior still converges to backend truth when SignalR is degraded.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Protect unaffected routes, finish the dark-theme terminal presentation, and capture validation evidence.

- [ ] T037 [P] Add sibling-route regression coverage for `/pets/market`, `/pets/leaderboard`, `/history/trades`, and `/history/settlements` in `apps/frontend-spa/src/features/trading-pets/__tests__/PetTradingRoutes.test.tsx`
- [X] T038 [P] Align terminal layout density, dark-theme tokens, and buy-vs-sell color hierarchy in `apps/frontend-spa/src/styles.css`
- [ ] T039 [P] Add regression assertions that existing pet-trading endpoints stay compatible alongside the new terminal surface in `tests/contract/market-service/MarketService.ContractTests/MarketApiContractsTests.cs`
- [ ] T040 Run the quickstart walkthrough and record PRF-001, PRF-002, and PRF-003 observations in `specs/005-pet-trading-terminal/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No dependencies; establish terminal file locations first.
- **Phase 2**: Depends on Phase 1 and blocks all story work.
- **Phase 3 (US1)**: Depends on Phase 2 only; this is the MVP.
- **Phase 4 (US2)**: Depends on Phase 3 because order submission must plug into the terminal workspace and read models.
- **Phase 5 (US3)**: Depends on Phase 3; realtime/highlighting can proceed in parallel with late US2 work once the workspace payloads are stable.
- **Phase 6**: Depends on all desired stories being complete.

### User Story Dependency Graph

```text
US1 (P1) ──► US2 (P2)
   │
   └──────► US3 (P3)
```

### Within Each User Story

- Tests should be written before or alongside implementation and should fail until the story behavior exists.
- Backend handlers should land before endpoint mapping is considered complete.
- Frontend route/components should consume the new terminal contracts instead of recreating business logic locally.
- Each story must preserve backend-authoritative state and explicit empty/error handling.

### Parallel Opportunities

- **Phase 1**: T001 and T002 can run in parallel.
- **Phase 2**: T004 and T006 can run in parallel once T003 defines shared contracts; T005 then fills the store implementation; T007 lands after handlers/endpoints exist.
- **US1**: T008-T010 can run in parallel; T014-T016 can run in parallel after the workspace DTOs are fixed.
- **US2**: T019-T021 can run in parallel; T025 can proceed once endpoint contracts are stable while T022-T024 finish on the backend.
- **US3**: T028-T030 can run in parallel; T034-T036 can run in parallel after T032 defines the highlight metadata shape.
- **Phase 6**: T037-T039 can run in parallel; T040 is the final validation pass.

---

## Parallel Example: User Story 1

```text
# Contract and UI tests in parallel:
Task T008 -> PetsTerminalMarketsContractTests.cs
Task T009 -> PetsTerminalWorkspaceContractTests.cs
Task T010 -> TraderWorkspacePage.test.tsx

# Once the workspace payload shape is stable:
Task T014 -> TerminalMarketList.tsx
Task T015 -> TerminalOrderBook.tsx
Task T016 -> TerminalTradeFeed.tsx
```

## Parallel Example: User Story 2

```text
# Backend and frontend test coverage together:
Task T019 -> PetsTerminalOrdersContractTests.cs
Task T020 -> PetTradingTerminalOrderFlowTests.cs
Task T021 -> TerminalTradingPanel.test.tsx

# After contract DTOs settle:
Task T025 -> tradingPetsApi.ts
Task T026 -> TerminalTradingPanel.tsx
```

## Parallel Example: User Story 3

```text
# Realtime/highlight test work:
Task T028 -> PetTradingTerminalRealtimeTests.cs
Task T029 -> tradingPetsRealtime.test.tsx
Task T030 -> TerminalHighlights.test.tsx

# UI emphasis work after highlight metadata exists:
Task T034 -> TerminalTradeFeed.tsx
Task T035 -> TerminalMarketList.tsx
Task T036 -> TerminalOrderBook.tsx
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2 so the terminal contracts, store methods, and workspace hook exist.
2. Complete Phase 3 (US1) to deliver the redesigned `/pets/workspace` terminal shell.
3. Stop and validate the US1 independent test before adding trading actions or realtime polish.

### Incremental Delivery

1. Deliver US1 for the backend-driven terminal layout and selected-market insight.
2. Add US2 so the same page can place ask, bid, and buy-now actions without leaving backend authority.
3. Add US3 to meet the live-terminal feel and performance fallback requirements.
4. Finish with Phase 6 regression coverage and quickstart evidence capture.

### Suggested MVP Scope

- **MVP**: Phase 1 + Phase 2 + Phase 3 (US1 only)
- **Next highest value**: Phase 4 (US2) for in-terminal execution
- **Final differentiation**: Phase 5 (US3) plus Phase 6 polish

---

## Notes

- `Buy Now` must remain fully backend-selected using best available asks; do not add client-side matching logic.
- Highlighting is presentation-only and must be derived from successive confirmed payloads, not speculative local inserts.
- Empty bid/ask/trade arrays are valid states and need explicit UI treatment.
- The feature scope remains limited to `/pets/workspace`; sibling pages should be regression-tested but not redesigned.
