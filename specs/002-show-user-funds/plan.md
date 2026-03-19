# Implementation Plan: Visible User Funds

**Branch**: `002-show-user-funds` | **Date**: 2026-03-19 | **Spec**: `specs/002-show-user-funds/spec.md`  
**Input**: Feature specification for making the authenticated user's available funds visible and live-updating across the trading experience.

## Summary

Expose the authenticated user's available funds in the existing authenticated header so the amount remains visible across primary trading screens, backed by a reusable account snapshot query and a client-side account state model. Keep the market service as the source of truth for demo account funds, update that projection when `TradeRecorded` confirms a balance-changing trade, and push `FundsUpdated` messages through the existing SignalR hub so the UI reflects confirmed changes without refresh.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend), TypeScript 5.x / React 18 (frontend), PowerShell (automation)  
**Primary Dependencies**: ASP.NET Core Minimal APIs, SignalR, EF Core (SqlServer), Auth0 SPA SDK, React Router, Vitest + Testing Library  
**Storage**: Existing `DemoAccounts` and `DemoHoldings` tables in the market service SQL-backed EF Core store  
**Testing**: xUnit + FluentAssertions (backend), contract + integration suites under `tests/`, Vitest + React Testing Library (frontend)  
**Target Platform**: Existing local demo stack and Azure Container Apps-hosted trading platform services with the React SPA  
**Project Type**: Monorepo web platform feature spanning one SPA and the market-service realtime/API layer  
**UX Consistency Baseline**: Reuse the current `app-header` auth panel, card layout, muted helper text, status pills, and explicit loading/error states already used across the SPA  
**Performance Goals**: Meet spec budgets: funds visible within 2 seconds of authenticated workspace readiness for 95% of logins, and confirmed trade-driven funds updates visible within 1 second for 95% of updates  
**Constraints**: Reuse existing contracts and SignalR hub where possible, do not introduce new infrastructure or libraries, keep the last confirmed amount visible during refreshes, and never show rejected or unconfirmed trades as final funds changes  
**Scale/Scope**: One active account per authenticated session, one always-visible funds summary on authenticated screens, one new account snapshot API, one new realtime funds event, and focused tests for market-service projection plus SPA display behavior

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality Gate**: PASS - Changes stay within existing bounded areas: frontend auth/header state, shared HTTP/realtime contracts, and market-service account projection/realtime publishing.
- **Testing Gate**: PASS - Plan adds frontend component/state tests, backend unit tests for funds projection math, contract tests for the new account query/event payload, and integration coverage for trade-to-funds realtime propagation.
- **UX Consistency Gate**: PASS - The display will reuse the authenticated header, existing typography and pill patterns, and explicit loading/updating/unavailable messaging aligned with current product copy.
- **Performance Gate**: PASS - The spec already defines measurable login and trade-update latency budgets; validation will use timed integration/manual scenarios plus operational telemetry review.
- **Simplicity Gate**: PASS - The design extends the existing market-service account model and existing SignalR hub instead of adding a new service, polling layer, or separate ledger subsystem.

## Design Direction

### Account Read Model

- Keep `DemoAccount` in market-service as the demo funds source of truth.
- Preserve `POST /api/accounts/me/bootstrap` for account creation/bootstrap-on-login.
- Add a side-effect-free `GET /api/accounts/me` snapshot endpoint so the client can refresh and retry without re-bootstrapping the account.

### Frontend Delivery Pattern

- Introduce a reusable authenticated account state layer that stores the last confirmed account snapshot and the current funds display state (`loading`, `confirmed`, `updating`, `unavailable`).
- Render the funds summary inside the existing header auth panel so the amount stays visible on all protected trading routes and remains consistent with the current shell layout.
- Keep the last confirmed amount visible during reconnects or delayed refreshes, with a clear status label and retry guidance when the fresh value cannot be confirmed.

### Realtime Update Pattern

- Extend the existing `/hubs/market` SignalR channel with a user-scoped `FundsUpdated` message rather than introducing a second hub.
- Update the market-service demo account projection when `TradeRecorded` is consumed, because that is the first confirmed lifecycle event that should change visible funds.
- Publish funds updates only after the new account snapshot is persisted so the UI never shows unconfirmed or rolled-back amounts.

### Validation Strategy

- Frontend: verify authenticated header visibility, loading/updating/unavailable states, and live refresh behavior from realtime messages and snapshot retries.
- Backend: verify account math for buyer/seller updates, no mutation on rejected/non-effective flows, and API/realtime contract shape.
- End-to-end: verify login shows funds within budget and confirmed trade activity updates the visible amount within budget.

## Project Structure

### Documentation (this feature)

```text
specs/002-show-user-funds/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── api-contracts.md
│   └── realtime-contracts.md
└── tasks.md
```

### Source Code (repository root)

```text
apps/
├── frontend-spa/
│   └── src/
│       ├── contracts/
│       ├── features/auth/
│       ├── features/market/
│       └── features/realtime/
└── services/
    └── market-service/
        └── src/
            ├── MarketService/
            ├── MarketService.Api/
            ├── MarketService.Application/
            └── MarketService.Infrastructure/

libs/
├── contracts/
│   ├── events/
│   └── http/
└── test-support/

tests/
├── unit/market-service/
├── contract/market-service/
├── integration/realtime/
└── integration/market-trade/
```

**Structure Decision**: Implement the feature as a focused cross-cutting enhancement to the existing SPA shell and market-service account/realtime layer, keeping shared contract changes in `libs/contracts` and validation in the existing frontend, contract, unit, and integration test suites.

## Post-Design Constitution Re-Check

- **Code Quality Gate**: PASS - The design keeps responsibilities explicit: account snapshot query, account projection update, realtime publish, and header display/state.
- **Testing Gate**: PASS - Unit, contract, integration, and frontend UI/state coverage are all mapped to the planned changes.
- **UX Consistency Gate**: PASS - The design reuses the established header shell and existing loading/error/status patterns instead of creating a new dashboard surface.
- **Performance Gate**: PASS - The design uses push-based SignalR updates plus snapshot refresh, which supports the required latency checks without introducing polling delay.
- **Simplicity Gate**: PASS - No new services, infrastructure, or third-party libraries are required; the plan extends current contracts and components only.

## Complexity Tracking

No constitution violations identified; no exception entries required.
