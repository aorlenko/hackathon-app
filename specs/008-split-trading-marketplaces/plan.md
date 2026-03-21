# Implementation Plan: Split trading into Primary supply and Resale marketplace

**Branch**: `008-split-trading-marketplaces` | **Date**: 2026-03-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-split-trading-marketplaces/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Split the current combined **pet trading workspace** (`TraderWorkspacePage` with `PrimaryMarketPanel` + `SecondaryMarketPanel`) into **two distinct routes**: **Primary supply market** (primary acquisition only) and **Resale marketplace** (peer listings with a **two-column layout**: the signed-in user’s active listings vs other participants’ offers). Navigation, page titles, and copy MUST match spec FR-002/FR-003; legacy `/pets/workspace` SHOULD redirect to the primary-supply destination so bookmarks and “pet trading” expectations remain recoverable. **No change to market-service business rules or HTTP contracts** is required: client partitions `GET /api/market/listings` by `sellerTraderId` vs current `traderId` (see `data-model.md`). Detailed routing and layout decisions are in `research.md`; API consumer contract reference is in `contracts/`; manual verification steps are in `quickstart.md`.

## Technical Context

**Language/Version**: TypeScript 5.x, React 18, Vite 6, React Router 6  
**Primary Dependencies**: Existing `apps/frontend-spa` trading feature (`tradingPetsApi.ts`, `MyPetTraderContext`, `useTradingPetsRealtime` / `hubInvalidateSeq`), Auth0 SPA SDK, `@microsoft/signalr`  
**Storage**: N/A (no new persistence; market service remains system of record per spec A-001/A-002)  
**Testing**: Vitest + Testing Library in `apps/frontend-spa` (`npm test`, `npm run lint`)  
**Target Platform**: Browser (desktop/tablet two-column; narrow viewports stack per spec A-003)  
**Project Type**: Web SPA (frontend-only information architecture and layout for this feature)  
**UX Consistency Baseline**: Existing `trading-pets` CSS variables, card/list patterns, `PrimaryMarketPanel` / `SecondaryMarketPanel` behaviors (006-pet-trading-ux)  
**Performance Goals**: Spec PRF-001 — usable skeleton or first meaningful content within **3 seconds** of navigation per page under QA assumptions; PRF-002/PRF-003 — repeatable QA checklist with documented exceptions if needed  
**Constraints**: FR-004 — own listings MUST NOT appear in the others’ column by default; partial column failure MUST not block the other column (spec edge cases)  
**Scale/Scope**: Two new or refactored pages under `apps/frontend-spa/src/features/trading-pets/`, `App.tsx` routes and `Header` nav updates, targeted tests and CSS for responsive two-column resale layout

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-design (initial)

- **Code Quality Gate**: **Pass** — changes stay within existing feature modules; lint/format via existing SPA toolchain; no new public backend surface.
- **Testing Gate**: **Pass** — unit tests for pure helpers (e.g. partition/list filters) and component tests for route-level layout, column separation, and empty/error states; integration covered by existing API mocks or MSW patterns if already used, else minimal RTL tests with mocked `getMarketListings`.
- **UX Consistency Gate**: **Pass** — reuse established trading copy, buttons, and empty/error patterns (UX-001–UX-003); document affected components in PR notes per UX-003.
- **Performance Gate**: **Pass** — spec PRF-001/PRF-002 define measurable budgets; `quickstart.md` records QA steps and device/network assumptions.
- **Simplicity Gate**: **Pass** — prefer extracting/reusing panels over new dependencies; server-side “my vs others” endpoints rejected unless client partition proves insufficient (see `research.md`).

### Post-design (Phase 1)

- **All gates**: **Pass** — `research.md` resolves routing and layout; `data-model.md` maps spec entities to existing DTOs; `contracts/` documents routes + unchanged HTTP surface; `quickstart.md` ties acceptance to manual checks. No complexity table entries required.

## Project Structure

### Documentation (this feature)

```text
specs/008-split-trading-marketplaces/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── http-market-api.md
│   └── spa-routing.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
apps/frontend-spa/
├── src/
│   ├── App.tsx                          # Routes + Header nav labels/paths
│   └── features/trading-pets/
│       ├── TraderWorkspacePage.tsx      # Replace, split, or redirect per research
│       ├── PrimaryMarketPanel.tsx       # Reuse on primary-supply page
│       ├── SecondaryMarketPanel.tsx     # Refactor or replace for resale-only + two columns
│       ├── tradingPetsApi.ts            # Unchanged unless proven necessary
│       └── __tests__/                   # New/updated tests for split + partition
└── src/styles.css                       # Responsive two-column resale layout (if needed)
```

**Structure Decision**: All implementation work targets **`apps/frontend-spa`** under `features/trading-pets` and **`App.tsx`**. **Market service** and shared libraries remain unchanged unless a follow-up spec extends APIs.

## Complexity Tracking

> No constitution violations required justification for this plan.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
