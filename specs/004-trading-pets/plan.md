# Implementation Plan: Trading Pets Platform

**Branch**: `004-trading-pets` | **Date**: 2026-03-20 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/004-trading-pets/spec.md`  
**Authoritative domain detail**: `system_reqs.md` (align formula and flows; treat spec + system_reqs as complementary)

**Note**: Filled by `/speckit.plan`. Template workflow: `.specify/templates/plan-template.md`.

## Summary

Deliver a **Trading Pets** domain on the existing monorepo: configurable **Traders** with private panels (cash, locks, inventory, portfolio, notifications), **Breed** catalog (20 breeds), **Pet** instances with lifecycle ticks and **intrinsic value** formula, **limited primary supply**, **secondary market as trading** (bid **≥** ask **executes immediately**; bid **&lt;** ask uses **single highest pending bid**, seller **accept/reject**, cash locking), **market / analysis / leaderboard** read surfaces, **notifications**, and **UI refresh within a few seconds** via SignalR (with a documented polling fallback). Implementation **reuses** C# / .NET 8 market-service, EF Core SQL, React SPA, Auth0, and existing harness/contract-test patterns; **pet mutations and projections** are owned primarily by **market-service** (see `research.md`).

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend), TypeScript 5.x / React 18 (frontend)  
**Primary Dependencies**: ASP.NET Core Minimal APIs, EF Core (SqlServer), SignalR, Auth0 SPA SDK + JWT bearer APIs, existing `Trading.*` shared libraries  
**Storage**: SQL Server via EF Core (new/extended tables for breeds, pets, supply, listings, bids, trades, notifications); seed breed rows from `system_reqs.md` §4 (see spec A-002)  
**Testing**: xUnit + `Trading.TestSupport` harness; contract tests under `tests/contract/market-service`; Vitest + Testing Library for SPA  
**Target Platform**: Same as repo: local dev + Azure-oriented deployment (Bicep/IaC as existing services)  
**Project Type**: Multi-service web (SPA + ASP.NET Core services); feature concentrates in `market-service` + `apps/frontend-spa`  
**UX Consistency Baseline**: Existing SPA layout, auth shell, and error messaging patterns; labels for available vs locked cash and bid/listing states must match across trader panel, market, and notifications (UX-001–UX-003)  
**Performance Goals**: PRF-001 — after purchase, bid, accept/reject, withdrawals, or valuation tick, required numeric/inventory updates visible on relevant panels within **≤ 5 seconds** under normal demo load; PRF-002 alignment with timed demo script in `system_reqs.md` §5  
**Constraints**: Sequential consistency per action is sufficient (FR-022); no silent failures on blocked actions (UX-002); private trader fields must never leak cross-trader (SC-003)  
**Scale/Scope**: Demo-oriented **N Traders** where **N is configurable** (any deployment-chosen count; **not** fixed to three—`system_reqs.md` §5 names A/B/C for examples only). **~20 breeds** from `system_reqs.md` §4; **default primary supply per breed** (often **3** in requirements) is **independent** of N. Volume bounded by manual/scripted demos; background valuation tick default **1 minute** (configurable)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-research

- **Code Quality Gate**: **PASS** — Backend: `dotnet format` / analyzers as in CI; frontend: `npm run lint`; new code split into Application/Domain/Api layers in market-service and feature folders in SPA.
- **Testing Gate**: **PASS** — Unit tests for intrinsic value + tick variance + cash-lock invariants; contract/API tests for primary buy, list, bid, outbid, withdraw bid, accept/reject, withdraw listing; SPA tests for critical panels with mocked realtime.
- **UX Consistency Gate**: **PASS** — Reuse existing cards/tables/shell; explicit copy for blocked actions; notification feed ordering and content per FR-021.
- **Performance Gate**: **PASS** — Budget: panel updates within 5s of mutating events (PRF-001); validate via harness or scripted timing + SignalR/poll fallback documented in `quickstart.md`.
- **Simplicity Gate**: **PASS** — Single-writer **market-service** aggregate for pet trading (see `research.md`); avoid extra microservice chatter unless a future showcase requires it.

### Post-design (after Phase 1 artifacts)

- **Code Quality / Tests / UX / Performance / Simplicity**: **PASS** — `data-model.md` and `contracts/` define bounded commands and reads; `quickstart.md` documents demo and verification path; no unresolved NEEDS CLARIFICATION in Technical Context.

## Project Structure

### Documentation (this feature)

```text
specs/004-trading-pets/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/           # Phase 1
│   ├── api-contracts.md
│   └── realtime-contracts.md
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit.tasks — not created here)
```

### Source Code (repository root)

```text
apps/services/market-service/src/
├── MarketService/              # Program.cs, hub mapping, DI
├── MarketService.Api/          # Minimal API endpoint groups (pets, traders, market, …)
├── MarketService.Application/  # Handlers, abstractions, domain services (valuation tick)
├── MarketService.Domain/       # Entities/value objects as needed
└── MarketService.Infrastructure/  # EF Core DbContext, migrations, stores

apps/frontend-spa/src/
├── features/                   # auth, market, … → extend/add trading-pets UI
├── App.tsx, routes, styles

libs/                           # Shared contracts (e.g. Trading.Contracts.Http) as needed

tests/contract/market-service/  # API contract tests against harness
libs/test-support/src/Trading.TestSupport/  # Harness + test doubles
```

**Structure Decision**: Extend **market-service** and **frontend-spa** as the primary delivery surface; keep trade/settlement services unchanged or idle unless an explicit demo needs them—pet trading logic does not require a second writable service for the stated consistency model.

## Complexity Tracking

> No constitution violations requiring justification for this feature plan.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
