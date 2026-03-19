# Tasks: Simplified Real-Time Trading Platform

**Input**: Design documents from `/specs/001-realtime-trading-platform/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Include unit, contract, integration, and smoke coverage to satisfy the plan quality gates and story-level independent tests.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no unmet dependencies)
- **[Story]**: User story label (`[US1]`, `[US2]`, `[US3]`) for story-phase tasks only
- Every task includes concrete file path(s)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize monorepo scaffolding, developer workflow, and baseline CI plumbing.

- [X] T001 Create monorepo folder skeleton in `apps/frontend-spa/.gitkeep`, `apps/services/market-service/.gitkeep`, `apps/services/trade-service/.gitkeep`, `apps/services/settlement-service/.gitkeep`, `libs/contracts/.gitkeep`, `libs/building-blocks/.gitkeep`, `libs/test-support/.gitkeep`, and `infra/bicep/modules/.gitkeep`
- [X] T002 Initialize .NET solution and service projects in `apps/services/TradingPlatform.sln`, `apps/services/market-service/src/MarketService/MarketService.csproj`, `apps/services/trade-service/src/TradeService/TradeService.csproj`, and `apps/services/settlement-service/src/SettlementService/SettlementService.csproj`
- [X] T003 [P] Initialize React + TypeScript SPA shell in `apps/frontend-spa/package.json`, `apps/frontend-spa/tsconfig.json`, and `apps/frontend-spa/src/main.tsx`
- [X] T004 [P] Add shared backend build settings in `apps/services/Directory.Build.props` and `apps/services/Directory.Packages.props`
- [X] T005 [P] Add root orchestration scripts in `package.json` and `scripts/dev/run-local.ps1`
- [X] T006 [P] Create baseline GitHub workflow skeleton in `.github/workflows/ci.yml`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build mandatory cross-cutting foundations required by all user stories.

**⚠️ CRITICAL**: Complete this phase before any user story implementation.

- [X] T007 Create shared configuration binding library in `libs/building-blocks/configuration/src/Trading.Configuration/ServiceCollectionExtensions.cs`
- [X] T008 [P] Implement Auth0 JWT validation helpers in `libs/building-blocks/auth/src/Trading.Auth/JwtAuthenticationExtensions.cs`
- [X] T009 [P] Implement Service Bus abstractions in `libs/building-blocks/messaging/src/Trading.Messaging/ServiceBusEventBus.cs`
- [X] T010 [P] Implement OpenTelemetry bootstrap extensions in `libs/building-blocks/observability/src/Trading.Observability/TelemetryExtensions.cs`
- [X] T011 Create shared HTTP DTO contracts in `libs/contracts/http/src/Trading.Contracts.Http/Orders.cs` and `libs/contracts/http/src/Trading.Contracts.Http/Markets.cs`
- [X] T012 [P] Create shared event contract definitions in `libs/contracts/events/src/Trading.Contracts.Events/LifecycleEvents.cs`
- [X] T013 Configure base API startup pipeline for Market Service in `apps/services/market-service/src/MarketService/Program.cs`
- [X] T014 [P] Configure base API startup pipeline for Trade Service in `apps/services/trade-service/src/TradeService/Program.cs`
- [X] T015 [P] Configure base API startup pipeline for Settlement Service in `apps/services/settlement-service/src/SettlementService/Program.cs`
- [X] T016 Create EF Core migration scaffolding in `apps/services/market-service/src/MarketService.Infrastructure/Persistence/Migrations/`, `apps/services/trade-service/src/TradeService.Infrastructure/Persistence/Migrations/`, and `apps/services/settlement-service/src/SettlementService.Infrastructure/Persistence/Migrations/`
- [X] T017 [P] Implement seed data loader for shared users/accounts/items in `apps/services/market-service/src/MarketService.Infrastructure/Seeding/SeedDataRunner.cs`
- [X] T018 [P] Add local composition profile for all apps and dependencies in `docker-compose.yml`
- [X] T019 [P] Add frontend API/auth environment contract in `apps/frontend-spa/src/config/env.ts`

**Checkpoint**: Foundation complete; user stories can start.

---

## Phase 3: User Story 1 - Place and Match Orders (Priority: P1) 🎯 MVP

**Goal**: A signed-in user submits buy/sell orders and matching produces trades with price-time and partial-fill behavior.

**Independent Test**: Two authenticated users place opposing orders and verify accepted orders, generated trades, and partial-fill handling.

### Tests for User Story 1

- [X] T020 [P] [US1] Add Market API contract tests for `POST /api/orders`, `GET /api/markets`, and `GET /api/markets/{symbol}/order-book` in `tests/contract/market-service/MarketApiContractsTests.cs`
- [X] T021 [P] [US1] Add matching engine unit tests for price-time and partial fills in `tests/unit/market-service/MatchingEngineTests.cs`
- [X] T022 [P] [US1] Add integration test for multi-user order matching flow in `tests/integration/market-trade/OrderToTradeFlowTests.cs`

### Implementation for User Story 1

- [X] T023 [P] [US1] Implement market entities for items/orders/audit in `apps/services/market-service/src/MarketService.Domain/Entities/Item.cs`, `Order.cs`, and `OrderMatchAudit.cs`
- [X] T024 [P] [US1] Implement market EF Core DbContext and default-schema table mapping in `apps/services/market-service/src/MarketService.Infrastructure/Persistence/MarketDbContext.cs`
- [X] T025 [US1] Implement order validation policy (tradable, bounds, account checks) in `apps/services/market-service/src/MarketService.Application/Orders/OrderValidationPolicy.cs`
- [X] T026 [US1] Implement price-time matching engine with partial fills in `apps/services/market-service/src/MarketService.Application/Matching/PriceTimeMatchingEngine.cs`
- [X] T027 [US1] Implement order placement command handler that persists state and emits events in `apps/services/market-service/src/MarketService.Application/Orders/PlaceOrderHandler.cs`
- [X] T028 [P] [US1] Implement market catalog and order-book query handlers in `apps/services/market-service/src/MarketService.Application/Markets/GetMarketsHandler.cs` and `GetOrderBookHandler.cs`
- [X] T029 [US1] Implement orders and markets endpoints in `apps/services/market-service/src/MarketService.Api/Endpoints/OrdersEndpoints.cs` and `MarketsEndpoints.cs`
- [X] T030 [US1] Implement `OrderPlaced` and `OrderMatched` publisher integration in `apps/services/market-service/src/MarketService.Infrastructure/Messaging/LifecycleEventPublisher.cs`
- [X] T031 [US1] Implement `OrderMatched` consumer and trade persistence in `apps/services/trade-service/src/TradeService.Application/Consumers/OrderMatchedConsumer.cs`
- [X] T032 [US1] Implement trade write model and persistence for immutable trades in `apps/services/trade-service/src/TradeService.Infrastructure/Persistence/TradeDbContext.cs` and `apps/services/trade-service/src/TradeService.Domain/Entities/Trade.cs`
- [X] T033 [US1] Implement `TradeRecorded` event publisher in `apps/services/trade-service/src/TradeService.Infrastructure/Messaging/TradeRecordedPublisher.cs`
- [X] T034 [US1] Add frontend order entry + market order-book view in `apps/frontend-spa/src/features/market/OrderEntryForm.tsx`, `OrderBookPanel.tsx`, and `apps/frontend-spa/src/features/market/marketApi.ts`

**Checkpoint**: US1 delivers end-to-end Order -> Trade creation and is independently testable.

---

## Phase 4: User Story 2 - Observe Lifecycle in Real Time (Priority: P2)

**Goal**: Market watchers receive automatic live updates for order-book, trades, and settlement transitions.

**Independent Test**: Two sessions run concurrently; one creates market activity while the other observes live updates without refresh.

### Tests for User Story 2

- [X] T035 [P] [US2] Add SignalR hub integration tests for subscribe/broadcast behavior in `tests/integration/realtime/MarketHubRealtimeTests.cs`
- [X] T036 [P] [US2] Add cross-service event relay integration test for trade and settlement notifications in `tests/integration/realtime/LifecycleRealtimeRelayTests.cs`
- [X] T037 [P] [US2] Add frontend live-update component tests in `apps/frontend-spa/src/features/realtime/__tests__/realtimeUpdates.test.tsx`

### Implementation for User Story 2

- [X] T038 [US2] Implement SignalR hub and market group membership in `apps/services/market-service/src/MarketService.Api/Hubs/MarketHub.cs`
- [X] T039 [US2] Wire order-book and trade event broadcasts from Market Service in `apps/services/market-service/src/MarketService.Application/Realtime/MarketRealtimeNotifier.cs`
- [X] T040 [US2] Implement `TradeRecorded` relay consumer for realtime push in `apps/services/market-service/src/MarketService.Application/Consumers/TradeRecordedRelayConsumer.cs`
- [X] T041 [US2] Implement settlement workflow consumer/state machine/events in `apps/services/settlement-service/src/SettlementService.Application/Consumers/TradeRecordedConsumer.cs` and `SettlementStateMachine.cs`
- [X] T042 [US2] Implement `SettlementStarted` and `SettlementCompleted` publishing in `apps/services/settlement-service/src/SettlementService.Infrastructure/Messaging/SettlementEventPublisher.cs`
- [X] T043 [US2] Implement settlement event relay consumer in Market Service in `apps/services/market-service/src/MarketService.Application/Consumers/SettlementRelayConsumer.cs`
- [X] T044 [US2] Implement frontend SignalR client and reconnect recovery in `apps/frontend-spa/src/features/realtime/marketHubClient.ts` and `apps/frontend-spa/src/features/realtime/useRealtimeLifecycle.ts`
- [X] T045 [US2] Update market detail UI to render live trades and settlement timeline in `apps/frontend-spa/src/features/market/RecentTradesPanel.tsx` and `SettlementStatusPanel.tsx`

**Checkpoint**: US2 provides near real-time lifecycle visibility with reconnect recovery.

---

## Phase 5: User Story 3 - Review History and Outcomes (Priority: P3)

**Goal**: Users can review historical trade executions and settlement outcomes with clear lifecycle context.

**Independent Test**: After at least one executed trade, user can query and view trade history and settlement outcomes in API and UI.

### Tests for User Story 3

- [X] T046 [P] [US3] Add Trade Service contract tests for `GET /api/trades` and `GET /api/users/{userId}/trades` in `tests/contract/trade-service/TradeApiContractsTests.cs`
- [X] T047 [P] [US3] Add Settlement Service contract tests for `GET /api/settlements/{tradeId}` and `GET /api/users/{userId}/settlements` in `tests/contract/settlement-service/SettlementApiContractsTests.cs`
- [X] T048 [P] [US3] Add end-to-end history retrieval integration test in `tests/integration/trade-settlement/HistoryAndOutcomesTests.cs`

### Implementation for User Story 3

- [X] T049 [US3] Implement trade query projections and repositories in `apps/services/trade-service/src/TradeService.Application/Queries/GetRecentTradesQuery.cs` and `GetUserTradesQuery.cs`
- [X] T050 [US3] Implement trade query endpoints in `apps/services/trade-service/src/TradeService.Api/Endpoints/TradesEndpoints.cs`
- [X] T051 [US3] Implement settlement query projections and repositories in `apps/services/settlement-service/src/SettlementService.Application/Queries/GetSettlementByTradeQuery.cs` and `GetUserSettlementsQuery.cs`
- [X] T052 [US3] Implement settlement query endpoints in `apps/services/settlement-service/src/SettlementService.Api/Endpoints/SettlementsEndpoints.cs`
- [X] T053 [US3] Implement frontend trade history page in `apps/frontend-spa/src/features/history/TradeHistoryPage.tsx` and `tradeHistoryApi.ts`
- [X] T054 [US3] Implement frontend settlement history page in `apps/frontend-spa/src/features/history/SettlementHistoryPage.tsx` and `settlementHistoryApi.ts`
- [X] T055 [US3] Add shared lifecycle trace UI component across history screens in `apps/frontend-spa/src/features/history/LifecycleTraceTable.tsx`

**Checkpoint**: US3 exposes understandable historical trade/settlement outcomes independently.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Harden quality gates, performance validation, and demo readiness across all stories.

- [X] T056 [P] Add Auth0 login/logout and protected route UX polish in `apps/frontend-spa/src/features/auth/AuthProvider.tsx` and `ProtectedRoute.tsx`
- [X] T056A Auto-provision a local demo trading account for first-time authenticated users in `apps/services/market-service/src/MarketService.Application/Orders/PlaceOrderHandler.cs`, `apps/services/market-service/src/MarketService.Infrastructure/Persistence/MarketDbContext.cs`, `tests/contract/market-service/MarketApiContractsTests.cs`, and `tests/integration/market-trade/OrderToTradeFlowTests.cs`
- [ ] T057 Add end-to-end smoke test for login -> order -> trade -> settlement in `tests/e2e/lifecycle-smoke.spec.ts`
- [X] T058 [P] Add latency/degraded-state telemetry checks in `libs/building-blocks/observability/src/Trading.Observability/LifecycleLatencyMetrics.cs`
- [X] T059 [P] Add Application Insights dashboard and alert templates in `infra/bicep/modules/monitoring.bicep` and `docs/observability/lifecycle-dashboard.md`
- [X] T060 Add Bicep main composition for ACA, SQL, Service Bus, Key Vault in `infra/bicep/main.bicep` and `infra/environments/hackathon/parameters.dev.json`
- [X] T061 [P] Finalize CI/CD workflow stages (lint, tests, image build, deploy, smoke) in `.github/workflows/ci.yml` and `.github/workflows/deploy-hackathon.yml`
- [X] T062 Add quickstart validation and demo runbook updates in `specs/001-realtime-trading-platform/quickstart.md` and `docs/demo/demo-runbook.md`

---

## Phase 7: Real Database Rollout

**Purpose**: Replace placeholder/local-only SQL assumptions with environment-specific real database usage across source-run development, Docker, and hosted Azure deployments.

- [ ] T063 Add SQL configuration profile validation coverage for source-run `.\SQLEXPRESS`, Docker `sqlserver`, and hosted Azure SQL resolution in `tests/integration/infrastructure/SqlConfigurationProfilesTests.cs`
- [ ] T064 Implement source-run local SQL Server defaults for `.\SQLEXPRESS` with Windows integrated authentication in `apps/services/market-service/src/MarketService/appsettings.Development.json`, `apps/services/trade-service/src/TradeService/appsettings.Development.json`, `apps/services/settlement-service/src/SettlementService/appsettings.Development.json`, and `libs/building-blocks/configuration/src/Trading.Configuration/ServiceCollectionExtensions.cs`
- [ ] T065 [P] Add repeatable local database bootstrap for `.\SQLEXPRESS` EF migrations and seed execution in `scripts/dev/init-sqlexpress.ps1` and `specs/001-realtime-trading-platform/quickstart.md`
- [ ] T066 [P] Harden Docker database wiring so containerized services use the Compose SQL image database and wait for migration-ready startup in `docker-compose.yml` and `scripts/dev/run-local.ps1`
- [ ] T067 Implement hosted Azure SQL connection-secret wiring for deployed services in `infra/bicep/main.bicep`, `infra/environments/hackathon/parameters.dev.json`, and `scripts/infra/deploy-hackathon.ps1`
- [ ] T068 [P] Update operator and developer runbooks for the local `.\SQLEXPRESS`, Docker SQL image, and hosted Azure SQL paths in `specs/001-realtime-trading-platform/quickstart.md` and `docs/demo/demo-runbook.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies.
- **Phase 2 (Foundational)**: Depends on Phase 1; blocks all stories.
- **Phase 3 (US1)**: Depends on Phase 2; MVP baseline for lifecycle creation.
- **Phase 4 (US2)**: Depends on Phase 2 and integrates with US1 lifecycle events.
- **Phase 5 (US3)**: Depends on Phase 2 and reads data produced by US1/US2.
- **Phase 6 (Polish)**: Depends on completion of selected stories (minimum US1 for MVP polish, all stories for full scope).
- **Phase 7 (Real Database Rollout)**: Depends on Phase 2 and existing SQL/migration scaffolding; recommended after MVP flow is stable and before hosted deployment validation.

### User Story Dependency Graph

- **US1 (P1)** -> foundational lifecycle creation (no story prerequisite after Phase 2)
- **US2 (P2)** -> depends on lifecycle events generated by US1 for meaningful realtime demos
- **US3 (P3)** -> depends on stored trade/settlement outcomes from US1 and settlement processing from US2

### Within-Story Ordering Rules

- Write tests before implementation tasks for each story.
- Implement domain/data models before handlers/services/endpoints.
- Integrate messaging/realtime after core persistence and business logic are in place.

### Parallel Opportunities

- Setup: T003, T004, T005, T006 can run in parallel after T001/T002 start.
- Foundational: T008, T009, T010, T012, T014, T015, T017, T018, T019 can run in parallel once base layout exists.
- US1: T020/T021/T022 parallel; T023/T024 parallel; T028 parallel with T025/T026 after entity setup.
- US2: T035/T036/T037 parallel; T044 and UI task T045 can proceed after hub contract is stable.
- US3: T046/T047/T048 parallel; T053 and T054 can run in parallel after API contract DTOs stabilize.
- Polish: T056, T058, T059, T061 can run in parallel.
- Real database rollout: T065, T066, and T068 can run in parallel after T064 establishes the environment-specific connection contract.

---

## Parallel Example: User Story 1

```bash
# Run US1 tests in parallel
Task: "T020 [US1] Market API contract tests in tests/contract/market-service/MarketApiContractsTests.cs"
Task: "T021 [US1] Matching engine unit tests in tests/unit/market-service/MatchingEngineTests.cs"
Task: "T022 [US1] Integration flow tests in tests/integration/market-trade/OrderToTradeFlowTests.cs"

# Build US1 domain/model layer in parallel
Task: "T023 [US1] Market entities in apps/services/market-service/src/MarketService.Domain/Entities/"
Task: "T024 [US1] Market DbContext in apps/services/market-service/src/MarketService.Infrastructure/Persistence/MarketDbContext.cs"
```

## Parallel Example: User Story 2

```bash
# Run US2 verification tasks in parallel
Task: "T035 [US2] SignalR hub integration tests in tests/integration/realtime/MarketHubRealtimeTests.cs"
Task: "T036 [US2] Event relay integration tests in tests/integration/realtime/LifecycleRealtimeRelayTests.cs"
Task: "T037 [US2] Frontend realtime tests in apps/frontend-spa/src/features/realtime/__tests__/realtimeUpdates.test.tsx"
```

## Parallel Example: User Story 3

```bash
# Execute US3 contract coverage in parallel
Task: "T046 [US3] Trade API contract tests in tests/contract/trade-service/TradeApiContractsTests.cs"
Task: "T047 [US3] Settlement API contract tests in tests/contract/settlement-service/SettlementApiContractsTests.cs"
Task: "T048 [US3] History integration test in tests/integration/trade-settlement/HistoryAndOutcomesTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1 and Phase 2.
2. Deliver Phase 3 (US1) and validate independent test criteria.
3. Demo Order -> Trade lifecycle before expanding scope.

### Incremental Delivery

1. Add US2 for realtime lifecycle visibility.
2. Add US3 for historical review and outcomes.
3. Complete Phase 6 for operational readiness and polished demo narrative.

### Suggested MVP Scope

- **MVP**: Phases 1-3 (through T034), then run US1 tests and smoke path.
- **Post-MVP**: Phases 4-6 for realtime richness, history, and cloud/demo hardening, then Phase 7 for environment-specific real database rollout.

