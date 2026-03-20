# Implementation Plan: Pet Trading Terminal Experience

**Branch**: `005-pet-trading-terminal` | **Date**: 2026-03-20 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/005-pet-trading-terminal/spec.md`

**Note**: Filled by `/speckit.plan`. Template workflow: `.specify/templates/plan-template.md`.

## Summary

Redesign only the existing `Pet Trading` workspace route into a dark, backend-driven trading terminal that shows a breed-centric market list, selected-market order book, trading panel, and recent trade feed without changing the underlying pet-trading business rules or any sibling pages. Delivery uses a terminal-specific read/command surface in `market-service` to aggregate the existing listings, bids, trades, trader snapshot, and SignalR invalidation flow into a breed-level workspace model, while the React SPA keeps `/pets/workspace` as the only page that materially changes.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend), TypeScript 5.x / React 18 (frontend)  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core (SqlServer), SignalR, Auth0 SPA SDK + JWT bearer APIs, React Router 6, Vitest + Testing Library, existing `Trading.*` shared libraries  
**Storage**: Existing market-service SQL Server store via EF Core; reuse current pet-trading tables/entities for breeds, pets, listings, bids, trades, and notifications, with terminal projections/read DTOs added in the same service as needed  
**Testing**: xUnit unit + contract + integration tests for `market-service`; Vitest + Testing Library for SPA route, hooks, and terminal data-shaping helpers  
**Target Platform**: Existing monorepo local-dev environment and current Azure-oriented deployment model for the SPA plus `market-service`  
**Project Type**: Monorepo web application (`apps/frontend-spa` + ASP.NET Core services under `apps/services`)  
**UX Consistency Baseline**: Reuse the current app shell, nav, dark theme, `trading-pets-*` CSS conventions, existing action labels, inline error handling, and trader/account wording already used across pet trading pages  
**Performance Goals**: Meet `PRF-001` to show visible updates within 3 seconds of backend confirmation for order-book, market, account, and trade-feed changes; validate with the `PRF-002` timed walkthrough and the `PRF-003` 90% success threshold while preserving last confirmed data when refresh misses budget  
**Constraints**: Change only `/pets/workspace`; keep Listings, Leaderboard, Trade History, and Settlement History behavior unchanged; keep backend as the single source of truth; do not simulate matching/settlement in the client; prefer additive read/command adapters over new dependencies or service boundaries  
**Scale/Scope**: Demo-scale pet markets with one selected breed/market entry at a time, tens of open listings and recent trades per breed, authenticated single-user workspace sessions, and contract-preserving coexistence with the existing pet-trading pages

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-research

- **Code Quality Gate**: **PASS** — Scope is isolated to `apps/frontend-spa/src/features/trading-pets`, `apps/frontend-spa/src/styles.css`, and `apps/services/market-service`; no new service boundary or dependency is required, and existing layered boundaries in `market-service` remain the extension point.
- **Testing Gate**: **PASS** — Plan includes frontend route/component tests, realtime hook tests, backend unit tests for terminal-order allocation logic, and backend contract/integration tests for new workspace reads plus unchanged legacy routes.
- **UX Consistency Gate**: **PASS** — The terminal will stay inside the existing Pet Ledger shell, preserve established terminology (`Pet trading`, listings, bids, balance, owned pets), and keep explicit empty/error states and dark-theme styling.
- **Performance Gate**: **PASS** — Budget is explicitly set by `PRF-001` through `PRF-003`; implementation will use SignalR invalidation with targeted refetches and stopwatch validation through the walkthrough.
- **Simplicity Gate**: **PASS** — The design extends current pet-trading APIs and projections inside `market-service` instead of introducing another service, client-side execution engine, or heavy state-management library.

### Post-design (after Phase 1 artifacts)

- **Code Quality / Tests / UX / Performance / Simplicity**: **PASS** — `research.md`, `data-model.md`, `contracts/`, and `quickstart.md` resolve the technical decisions, keep the feature additive to existing pet-trading logic, and define measurable validation for regressions, UX consistency, and 3-second refresh behavior.

## Project Structure

### Documentation (this feature)

```text
specs/005-pet-trading-terminal/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── terminal-api-contracts.md
│   └── realtime-contracts.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
apps/services/market-service/src/
├── MarketService/                  # Program.cs, hub mapping, DI
├── MarketService.Api/              # Minimal API endpoint groups and realtime publisher
├── MarketService.Application/      # Terminal read models and command orchestration
├── MarketService.Domain/           # Existing pet-trading entities/rules remain authoritative
└── MarketService.Infrastructure/   # EF Core stores/projections for listings, bids, trades

apps/frontend-spa/src/
├── App.tsx
├── styles.css
└── features/trading-pets/
    ├── TraderWorkspacePage.tsx
    ├── tradingPetsApi.ts
    ├── useTradingPetsRealtime.ts
    ├── __tests__/
    └── terminal/                   # New terminal-specific view components/helpers if needed

libs/                               # Shared contracts/building blocks if a shared DTO becomes necessary

tests/
├── contract/market-service/
├── integration/
└── unit/
```

**Structure Decision**: Extend the existing `market-service` and `apps/frontend-spa` feature folder only. The terminal-specific backend surface stays inside `market-service` as an additive API/read-model layer over current pet-trading rules, and the frontend keeps the same route/provider shell so non-target pages remain intact.

## Complexity Tracking

> No constitution violations requiring justification for this feature plan.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
