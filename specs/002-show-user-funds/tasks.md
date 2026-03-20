# Tasks: Visible User Funds

**Input**: Design documents from `/specs/002-show-user-funds/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Lean test tasks are included for the hackathon MVP so the core login and trade-update flows have basic coverage without expanding into resilience-heavy scenarios.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (`[US1]`, `[US2]`)
- Include exact file paths in every task description

## Phase 1: Setup (Shared Feature Scaffolding)

**Purpose**: Establish the shared contracts and feature entry points needed by all user stories.

- [x] T001 Add account snapshot and funds realtime DTOs in `libs/contracts/http/src/Trading.Contracts.Http/Accounts.cs` and `libs/contracts/http/src/Trading.Contracts.Http/Realtime.cs`
- [x] T002 [P] Extend frontend account and realtime types in `apps/frontend-spa/src/contracts/trading.ts`
- [x] T003 [P] Create account feature scaffolding in `apps/frontend-spa/src/features/account/accountApi.ts`, `apps/frontend-spa/src/features/account/useAccountFunds.ts`, and `apps/frontend-spa/src/features/account/FundsPanel.tsx`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared API, realtime, and test harness plumbing that every story depends on.

**⚠️ CRITICAL**: No user story work should begin until this phase is complete.

- [x] T004 Create the account snapshot query handler in `apps/services/market-service/src/MarketService.Application/Accounts/GetCurrentAccountHandler.cs`
- [x] T005 Add the authenticated `GET /api/accounts/me` endpoint and shared account response mapping in `apps/services/market-service/src/MarketService.Api/Endpoints/AccountsEndpoints.cs`
- [x] T006 [P] Extend realtime publishing abstractions for user-scoped `FundsUpdated` messages in `apps/services/market-service/src/MarketService.Application/Realtime/MarketRealtimeNotifier.cs` and `libs/test-support/src/Trading.TestSupport/Realtime/CollectingMarketHubPublisher.cs`
- [x] T007 [P] Add account user-group membership and `FundsUpdated` hub plumbing in `apps/services/market-service/src/MarketService.Api/Hubs/MarketHub.cs`
- [x] T008 [P] Add snapshot refresh helpers in `apps/frontend-spa/src/features/account/accountApi.ts` and `apps/frontend-spa/src/features/auth/authApi.ts`
- [x] T009 [P] Expose account snapshot and funds realtime test helpers in `libs/test-support/src/Trading.TestSupport/TradingPlatformHarness.cs`

**Checkpoint**: Shared account snapshot and realtime infrastructure is ready for story work.

---

## Phase 3: User Story 1 - See Funds Immediately After Login (Priority: P1) 🎯 MVP

**Goal**: Show the authenticated user's available funds in the persistent trading header as soon as the workspace is ready.

**Independent Test**: Sign in to an account with demo funds and confirm the header shows a clearly labeled funds value or a clear loading state without navigating away or refreshing.

### Tests for User Story 1

- [x] T010 [P] [US1] Add header funds visibility and initial loading state tests in `apps/frontend-spa/src/features/account/__tests__/fundsHeader.test.tsx`
- [x] T011 [P] [US1] Add contract coverage for `POST /api/accounts/me/bootstrap` and `GET /api/accounts/me` in `tests/contract/market-service/MarketService.ContractTests/MarketApiContractsTests.cs`

### Implementation for User Story 1

- [x] T012 [P] [US1] Implement account snapshot fetching and formatting helpers in `apps/frontend-spa/src/features/account/accountApi.ts` and `apps/frontend-spa/src/features/account/useAccountFunds.ts`
- [x] T013 [P] [US1] Hydrate authenticated account funds state from login/bootstrap in `apps/frontend-spa/src/features/auth/AuthProvider.tsx`
- [x] T014 [US1] Render the funds summary inside the persistent authenticated header in `apps/frontend-spa/src/App.tsx` and `apps/frontend-spa/src/features/account/FundsPanel.tsx`
- [x] T015 [US1] Style the confirmed and loading funds display states in `apps/frontend-spa/src/styles.css`

**Checkpoint**: User Story 1 is independently functional when funds are visible immediately after login across the authenticated shell.

---

## Phase 4: User Story 2 - Watch Funds Change While Trading (Priority: P1)

**Goal**: Update the visible funds amount automatically when confirmed trades change the active account balance.

**Independent Test**: Place balance-changing trades and confirm the header funds update within the active session without a manual refresh or new login, while rejected trades leave the confirmed amount unchanged.

### Tests for User Story 2

- [x] T016 [P] [US2] Add buyer and seller funds projection unit tests in `tests/unit/market-service/MarketService.UnitTests/DemoAccountFundsProjectionTests.cs`
- [x] T017 [P] [US2] Add realtime contract coverage for the `FundsUpdated` envelope in `tests/contract/market-service/MarketService.ContractTests/MarketRealtimeContractsTests.cs`
- [x] T018 [P] [US2] Add realtime integration coverage for `FundsUpdated` publishing in `tests/integration/realtime/Realtime.IntegrationTests/LifecycleRealtimeRelayTests.cs`
- [x] T019 [P] [US2] Add SPA realtime funds update coverage in `apps/frontend-spa/src/features/realtime/__tests__/realtimeUpdates.test.tsx` and `apps/frontend-spa/src/features/account/__tests__/fundsHeader.test.tsx`

### Implementation for User Story 2

- [x] T020 [P] [US2] Implement confirmed trade funds projection logic in `apps/services/market-service/src/MarketService.Application/Accounts/ApplyTradeToAccountsHandler.cs` and `apps/services/market-service/src/MarketService.Application/Abstractions/IMarketDataStore.cs`
- [x] T021 [US2] Apply buyer and seller account mutations when `TradeRecorded` is consumed in `apps/services/market-service/src/MarketService.Application/Consumers/TradeRecordedRelayConsumer.cs`
- [x] T022 [US2] Publish persisted account snapshots through user-scoped `FundsUpdated` messages in `apps/services/market-service/src/MarketService.Application/Realtime/MarketRealtimeNotifier.cs` and `apps/services/market-service/src/MarketService.Api/Hubs/MarketHub.cs`
- [x] T023 [US2] Update client account state from `FundsUpdated` events in `apps/frontend-spa/src/features/realtime/marketHubClient.ts` and `apps/frontend-spa/src/features/account/useAccountFunds.ts`

**Checkpoint**: User Story 2 is independently functional when confirmed trades update the visible funds automatically and non-effective trade attempts do not.

---

## Phase 5: Hackathon Validation

**Purpose**: Keep final verification lightweight and focused on the demoable outcome.

- [x] T024 [P] Run and document the MVP verification flow in `specs/002-show-user-funds/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup**: No dependencies and can start immediately.
- **Phase 2: Foundational**: Depends on Phase 1 and blocks all user stories.
- **Phase 3: User Story 1**: Depends on Phase 2 and delivers the MVP.
- **Phase 4: User Story 2**: Depends on Phase 2 and can proceed in parallel with Phase 3 once the shared account infrastructure exists.
- **Phase 5: Hackathon Validation**: Depends on `US1` and `US2`.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational and delivers the first usable funds experience.
- **US2 (P1)**: Starts after Foundational and reuses the shared account snapshot and realtime plumbing rather than depending on US1 implementation order.
### Within Each User Story

- Write the listed tests first and confirm they fail for the missing behavior.
- Complete shared data or state work before UI or event wiring that depends on it.
- Finish the story and validate its independent test before moving to hackathon validation.

### Suggested Completion Order

- `Setup -> Foundational -> US1 -> US2 -> Hackathon Validation`
- If multiple contributors are available after Foundational, `US1` and `US2` can be split across contributors with coordination on shared files.

---

## Parallel Opportunities

- `T002` and `T003` can run in parallel after `T001`.
- `T006`, `T007`, `T008`, and `T009` can run in parallel after `T004` and `T005`.
- `T010` and `T011` can run in parallel for US1.
- `T012` and `T013` can run in parallel once the US1 tests are in place.
- `T016`, `T017`, `T018`, and `T019` can run in parallel for US2.
- `T020` can proceed in parallel with US2 test authoring, then `T021` through `T023` follow in order.
- `T024` can run once `US1` and `US2` are demo-ready.

---

## Parallel Example: User Story 1

```bash
Task: "Add header funds visibility and initial loading state tests in apps/frontend-spa/src/features/account/__tests__/fundsHeader.test.tsx"
Task: "Add contract coverage for POST /api/accounts/me/bootstrap and GET /api/accounts/me in tests/contract/market-service/MarketService.ContractTests/MarketApiContractsTests.cs"

Task: "Implement account snapshot fetching and formatting helpers in apps/frontend-spa/src/features/account/accountApi.ts and apps/frontend-spa/src/features/account/useAccountFunds.ts"
Task: "Hydrate authenticated account funds state from login/bootstrap in apps/frontend-spa/src/features/auth/AuthProvider.tsx"
```

## Parallel Example: User Story 2

```bash
Task: "Add buyer and seller funds projection unit tests in tests/unit/market-service/MarketService.UnitTests/DemoAccountFundsProjectionTests.cs"
Task: "Add realtime contract coverage for the FundsUpdated envelope in tests/contract/market-service/MarketService.ContractTests/MarketRealtimeContractsTests.cs"
Task: "Add realtime integration coverage for FundsUpdated publishing in tests/integration/realtime/Realtime.IntegrationTests/LifecycleRealtimeRelayTests.cs"
Task: "Add SPA realtime funds update coverage in apps/frontend-spa/src/features/realtime/__tests__/realtimeUpdates.test.tsx and apps/frontend-spa/src/features/account/__tests__/fundsHeader.test.tsx"
```

## Implementation Strategy

### Hackathon MVP

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Complete Phase 4: User Story 2.
5. Finish with Phase 5: Hackathon Validation.

### Suggested MVP Scope

- Phase 1
- Phase 2
- Phase 3 (`US1`)
- Phase 4 (`US2`)

---

## Notes

- All tasks follow the required checklist format: checkbox, task ID, optional `[P]`, required `[US#]` labels for story tasks, and exact file paths.
- This trimmed plan is optimized for a hackathon demo: immediate funds visibility plus live updates after confirmed trades.
- `US3` delayed/unavailable-state hardening is intentionally deferred unless extra time remains.
