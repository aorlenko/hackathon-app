# Implementation Plan: Simplified Real-Time Trading Platform

**Branch**: `001-realtime-trading-platform` | **Date**: 2026-03-13 | **Spec**: `specs/001-realtime-trading-platform/spec.md`  
**Input**: Approved feature specification for a simplified real-time trading platform plus hackathon stack constraints.

## Summary

Deliver a minimal but complete vertical slice of `Order -> Trade -> Settlement` with live UI updates first, then add observability, robustness, and demo polish. Use a monorepo with one React SPA and three .NET 8 backend services deployed to a single Azure Container Apps environment, connected via Azure SQL Database, Azure Service Bus, and SignalR, with Auth0-based authentication and Bicep-managed infrastructure.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend), TypeScript 5.x / React 18 (frontend), Bicep (IaC), PowerShell (automation)  
**Primary Dependencies**: ASP.NET Core Minimal APIs, SignalR, EF Core (SqlServer), Azure Service Bus SDK, Azure Identity, Auth0 SPA + JWT APIs, OpenTelemetry + Application Insights exporter  
**Storage**: Azure SQL Database with EF Core migrations and default-schema tables  
**Testing**: xUnit + FluentAssertions (backend), contract/integration tests with Testcontainers where practical, Vitest + React Testing Library (frontend), Playwright smoke tests for demo path  
**Target Platform**: Azure Container Apps (single environment), Azure SQL Database, Azure Service Bus, Azure Key Vault, Azure Monitor/Application Insights  
**Project Type**: Monorepo web platform (SPA + event-driven backend services)  
**UX Consistency Baseline**: Shared trading vocabulary, reusable page layout/components, consistent loading/error/empty states, lifecycle trace visibility across market/trade/settlement views  
**Performance Goals**: Orders and trades visible in UI within 2s, settlement updates within 3s, support 50 concurrent demo users and 20 items under demo conditions  
**Constraints**: Hackathon-first speed, high demo value, parallel AI-agent implementation, twist-readiness via configurable rules, avoid enterprise-only complexity  
**Scale/Scope**: One demo environment, seeded data, three backend services and one frontend deployable, no production exchange requirements

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality Gate**: PASS - Enforce lint/format/static analysis and clear module boundaries per service and shared contracts.
- **Testing Gate**: PASS - Unit tests for matching/settlement logic plus contract/integration coverage for API and events; CI enforces pass.
- **UX Consistency Gate**: PASS - Shared terminology and lifecycle trace requirements mapped to frontend routes/components and acceptance checks.
- **Performance Gate**: PASS - Explicit latency budgets from spec with validation via telemetry + scripted load and smoke scenarios.
- **Simplicity Gate**: PASS - Three services only, single SQL DB, single ACA env, minimum viable event-driven flow; deferred non-essential features.

## Delivery Strategy

### 1) Implementation Sequence for Fast Demo

1. Monorepo scaffolding, shared contracts, local compose/profile, baseline CI.
2. Auth flow (Auth0 SPA login + API JWT validation) and seeded catalog/accounts.
3. Market Service minimal APIs + order acceptance + in-memory or SQL-backed matching engine.
4. Trade Service trade recording + `TradeRecorded` publication.
5. Settlement Service async workflow with simulated success/failure and state transitions.
6. SignalR integration and frontend live views (order book, recent trades, settlement status).
7. Infra modules + cloud deploy + end-to-end demo script + telemetry dashboards.

### 2) Minimal End-to-End Flow First

- Signed-in user places order.
- Market Service validates and stores order, matches opposite side by price-time.
- Trade event is emitted and persisted by Trade Service.
- Settlement Service starts and completes/ fails simulated settlement.
- SignalR pushes order/trade/settlement updates to SPA in near real time.

### 3) Essential vs Optional

- **Essential**: Auth, order placement, matching, trade persistence, settlement lifecycle, live updates, seeded data, cloud deployment, CI validation.
- **Optional Polish**: Advanced charting, rich filtering/sorting, optimistic UI refinements, operator dashboard, chaos/perf tuning beyond demo target.

## Solution Structure

### Repository / Monorepo Layout

```text
apps/
├── frontend-spa/                      # React + TypeScript SPA
└── services/
    ├── market-service/                # Order intake, order book, matching
    ├── trade-service/                 # Trade persistence and query APIs
    └── settlement-service/            # Settlement workflow and status APIs

libs/
├── contracts/
│   ├── http/                          # OpenAPI docs and shared DTO definitions
│   └── events/                        # Event schemas/versioning docs
├── building-blocks/
│   ├── observability/                 # Logging, tracing, metrics bootstrap
│   ├── messaging/                     # Service Bus publisher/consumer helpers
│   ├── auth/                          # Auth0/JWT shared helpers
│   └── configuration/                 # Key Vault/config composition
└── test-support/
    ├── fixtures/
    └── contract-test-kit/

infra/
├── bicep/
│   ├── modules/
│   └── main.bicep
└── environments/
    └── hackathon/

.github/workflows/
docs/
```

### Main Projects and Shared Building Blocks

- `frontend-spa`: Login, market overview/detail, order entry, trade history, settlement views, SignalR client.
- `market-service`: Order APIs, validation, matching logic, order book query, publishes order/trade-intent events, pushes live market updates.
- `trade-service`: Consumes matched-order events, persists trades, exposes trade read APIs, publishes `TradeRecorded`.
- `settlement-service`: Consumes `TradeRecorded`, runs settlement simulation/state machine, publishes settlement state events.
- Shared contracts in `libs/contracts` prevent drift across parallel AI-generated modules.

## Service Implementation Plan

### Market Service

- **Purpose**: Handle order intake and matching as the system entry point.
- **Core Responsibilities**: Validate order rules, persist open orders, maintain item order books, execute price-time matching, emit match events, expose order-book reads.
- **Key Interfaces**:
  - `POST /api/orders`
  - `GET /api/markets/{symbol}/order-book`
  - `GET /api/markets`
  - Publishes `OrderPlaced` and `OrderMatched`
- **Owned Data**: `Items`, `Orders`, `OrderMatchAudits`, `DemoAccounts`, `DemoHoldings`
- **Dependencies**: Auth0 JWT validation, SQL DB, Service Bus, SignalR notifier abstraction
- **Build First**: Input validation + order persistence + simple matching + order-book query + `OrderMatched` event

### Trade Service

- **Purpose**: System of record for executed trades and trade history queries.
- **Core Responsibilities**: Consume match events, create immutable trade records, expose trade history endpoints, publish lifecycle event for settlement.
- **Key Interfaces**:
  - `GET /api/trades?symbol={symbol}&limit={n}`
  - `GET /api/users/{userId}/trades`
  - Consumes `OrderMatched`
  - Publishes `TradeRecorded`
- **Owned Data**: `Trades`
- **Dependencies**: Service Bus, SQL DB, Market contract DTOs, SignalR notifier abstraction
- **Build First**: Consumer + persistence + trade query endpoint + `TradeRecorded` publish

### Settlement Service

- **Purpose**: Simulate post-trade cash/asset settlement state transitions.
- **Core Responsibilities**: Start settlement on trade, apply configurable policy, update account balances/holdings, track terminal state (Settled/Failed), expose status.
- **Key Interfaces**:
  - `GET /api/settlements/{tradeId}`
  - `GET /api/users/{userId}/settlements`
  - Consumes `TradeRecorded`
  - Publishes `SettlementStarted` and `SettlementCompleted`
- **Owned Data**: `Settlements`
- **Dependencies**: Service Bus, SQL DB, Key Vault/config, optional call to market/trade read APIs only when needed
- **Build First**: State machine + handler for `TradeRecorded` + basic status read API

### Frontend SPA

- **Purpose**: Demo-facing interface showing full lifecycle in real time.
- **Core Responsibilities**: Auth flow, market list/detail, order entry, live order book, recent trades, settlement status timeline.
- **Key Interfaces**:
  - Calls market/trade/settlement APIs
  - Connects to SignalR hub `/hubs/market`
- **Owned Data**: Client state only (no persistent ownership)
- **Dependencies**: Auth0 SPA SDK, backend APIs, SignalR, shared TypeScript contracts
- **Build First**: Login + market detail view + order form + live panels for order book/trades/settlement

## Event-Driven Workflow Plan

1. **Order Placed**
   - Published by: Market Service (`OrderPlaced`)
   - Consumed by: Frontend real-time broadcaster (for order book), optional audit stream
2. **Order Matched**
   - Published by: Market Service (`OrderMatched`)
   - Consumed by: Trade Service (mandatory), frontend broadcaster (optional low-latency preview)
3. **Trade Recorded**
   - Published by: Trade Service (`TradeRecorded`)
   - Consumed by: Settlement Service (mandatory), frontend broadcaster (recent trades)
4. **Settlement Started**
   - Published by: Settlement Service (`SettlementStarted`)
   - Consumed by: Frontend broadcaster, monitoring/audit
5. **Settlement Completed**
   - Published by: Settlement Service (`SettlementCompleted` with terminal state field)
   - Consumed by: Frontend broadcaster, optional account projection updater

Design rule: events are append-only and versioned (`eventType`, `eventVersion`, `occurredAt`, `correlationId`) to keep late twist changes backward-compatible.

## Database Plan

- **Platform Choice**: Azure SQL Database (single logical server + single database).
- **Ownership Model**: Service-specific EF Core DbContexts own their own default-schema tables and migrations.
- **Why this model**: Keeps persistence code-first, avoids handwritten SQL drift, and preserves modularity without schema coupling.
- **Initial Entities to Implement First**:
  - `DemoAccounts`, `DemoHoldings`, `Items`
  - `Orders`
  - `Trades`
  - `Settlements`

## Real-Time Update Plan

- **SignalR Hub Location**: Host one hub in Market Service (`/hubs/market`) to keep client connection simple.
- **Channels/Groups**:
  - `market:{symbol}:orderbook`
  - `market:{symbol}:trades`
  - `user:{userId}:settlements`
- **Update Triggers**:
  - Market Service pushes order book updates directly on order acceptance/match.
  - Trade Service publishes `TradeRecorded`; Market Service subscribes (or a small relay worker) and pushes recent trades updates.
  - Settlement Service publishes settlement events; Market Service relay pushes user settlement updates.
- **Fallback Strategy**: On reconnect, SPA refreshes current snapshots via REST endpoints before resuming stream updates.

## Infrastructure Plan

Hackathon-friendly single-topology Azure design via Bicep:

- Resource group (`rg-trading-hackathon-{env}`)
- One Container Apps environment
- Container Apps:
  - `market-service`
  - `trade-service`
  - `settlement-service`
  - `frontend-spa` (Nginx static container for single deployment pattern)
- Azure Container Registry for images
- Azure SQL logical server + single database
- Azure Service Bus namespace + topics/subscriptions for lifecycle events
- Azure Key Vault for Auth0 secrets, SQL connection, Service Bus connection fallback settings
- Application Insights + Log Analytics workspace + Azure Monitor alerts
- Managed identities per container app with RBAC/Key Vault access policies
- Ingress public for frontend + market API gateway endpoint; internal service-to-service when possible

## CI/CD Plan

Lightweight Git-based pipelines (GitHub Actions or Azure DevOps with equivalent stages):

- **PR Validation**:
  - changed-files scoped lint/format/test
  - contract checks (OpenAPI/event schema validation)
  - Bicep `build` + lint validation
- **Build/Test**:
  - backend unit/integration tests
  - frontend unit tests + smoke e2e
- **Image Build/Push**:
  - build container images for 3 services + frontend
  - push to ACR tagged by commit SHA + branch alias
- **Infra Validation/Deploy**:
  - Bicep `what-if` on main branch or manual dispatch
  - deploy to hackathon environment after approval
- **App Deployment**:
  - update ACA revisions with new images
  - run post-deploy smoke test: login -> order -> trade -> settlement -> live update visible
- **Frontend Deployment**:
  - bundled with image deploy in ACA to stay consistent and simple

## AI Agent Work Breakdown

Parallel workstreams optimized for Cursor agent boundaries:

1. **Scaffolding**
   - Objective: monorepo skeleton, shared libs, developer scripts, baseline CI
   - Boundaries: no domain logic
   - Outputs: folder structure, solution/workspace files, build scripts
   - Dependencies: none
2. **Auth**
   - Objective: Auth0 integration across SPA and APIs
   - Boundaries: auth middleware/guards and login UX only
   - Outputs: login/logout flow, JWT validation, protected endpoints
   - Dependencies: scaffolding
3. **Market Service**
   - Objective: order intake, order book, matching, market events
   - Boundaries: market-owned persistence and matching only, no settlement logic
   - Outputs: APIs, matching engine, tests, `OrderPlaced`/`OrderMatched`
   - Dependencies: scaffolding, auth contracts
4. **Trade Service**
   - Objective: consume matches, persist/query trades, publish `TradeRecorded`
   - Boundaries: trade-owned persistence only
   - Outputs: consumer, APIs, event publisher, tests
   - Dependencies: scaffolding, market event contract
5. **Settlement Service**
   - Objective: process `TradeRecorded`, settlement state machine, terminal status
   - Boundaries: settlement-owned persistence only
   - Outputs: consumer, APIs, settlement events, tests
   - Dependencies: scaffolding, trade event contract
6. **Frontend**
   - Objective: demo UI for lifecycle visualization and order entry
   - Boundaries: client-side only; no backend logic
   - Outputs: pages/components, API clients, SignalR integration
   - Dependencies: auth + market APIs + event payload contracts
7. **Infra**
   - Objective: Bicep modules and environment deployment scripts
   - Boundaries: infra only
   - Outputs: deployable templates, parameter files, docs
   - Dependencies: service names/images agreed
8. **CI/CD**
   - Objective: PR/build/deploy pipelines
   - Boundaries: workflow config and scripts only
   - Outputs: workflow YAML, quality gates, smoke automation
   - Dependencies: scaffolding and infra basics
9. **Observability/Demo Polish**
   - Objective: dashboards, traces, alert basics, scripted demo flow
   - Boundaries: no core domain behavior changes
   - Outputs: telemetry wiring, dashboard JSON/queries, demo checklist
   - Dependencies: all services integrated

## Risk Reduction / Twist Readiness

- Keep matching and settlement policies behind interfaces and config-driven rule sets.
- Use event versioning and additive schema evolution; avoid breaking contract changes.
- Externalize business parameters (bounds, settlement delay/failure rate, matching tweaks) into Key Vault/App Config values.
- Add feature flags for optional flows (partial fills strategy, settlement failure simulation, UI widgets).
- Avoid hardcoded item/account assumptions; rely on seed scripts and config files.
- Keep service boundaries thin and explicit to permit replacing one service without rewriting others.
- Maintain deterministic demo scripts and seed reset commands for fast recovery after late changes.

## Definition of Done

- Local development runs with one command profile (frontend + three services + dependencies).
- Cloud deployment runs in Azure with Bicep-provisioned resources and successful app rollout.
- Auth0 login works end-to-end; unauthorized requests are blocked correctly.
- Order -> Trade -> Settlement lifecycle completes successfully in demo path.
- SignalR real-time updates for order book, trades, and settlement are visible in SPA.
- EF Core migrations and seed data flows are repeatable.
- CI/CD validates PRs, builds/tests images, validates/deploys Bicep, and deploys apps.
- Baseline observability confirms latency budgets and surfaces degraded state signals.

## Project Structure

### Documentation (this feature)

```text
specs/001-realtime-trading-platform/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── api-contracts.md
│   └── event-contracts.md
└── tasks.md
```

### Source Code (repository root)

```text
apps/
├── frontend-spa/
└── services/
    ├── market-service/
    ├── trade-service/
    └── settlement-service/

libs/
├── contracts/
├── building-blocks/
└── test-support/

infra/
└── bicep/

.github/workflows/
```

**Structure Decision**: Use a monorepo with bounded service folders and shared contract/building-block libraries to maximize AI-agent parallelization while preserving simple integration.

## Post-Design Constitution Re-Check

- **Code Quality Gate**: PASS - Service boundaries, shared contracts, and lint/static checks included in plan.
- **Testing Gate**: PASS - Unit + integration/contract + smoke test layers defined.
- **UX Consistency Gate**: PASS - Shared terminology and lifecycle trace explicitly required.
- **Performance Gate**: PASS - Latency targets and telemetry-backed validation approach defined.
- **Simplicity Gate**: PASS - Single env/db and limited service count intentionally chosen.

## Complexity Tracking

No constitution violations identified; no exception entries required.
