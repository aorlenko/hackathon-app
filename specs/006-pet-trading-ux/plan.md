# Implementation Plan: Pet Trading Marketplace UX

**Branch**: `006-pet-trading-ux` | **Date**: 2026-03-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-pet-trading-ux/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Redesign the **Pet Trading workspace** (`/pets/workspace`) into a clear **pet marketplace** layout: prominent **cash / locked / portfolio** summary, four visually separated areas (primary supply, resale, owned pets, recent activity), improved **listing and inventory** readability, and calmer **loading / empty / error** states—**without** changing trading rules or substituting mock data. Implementation stays in the React SPA (`apps/frontend-spa` feature `trading-pets`) and shared `styles.css`, reusing existing `tradingPetsApi.ts` and realtime refresh behavior. Research and contracts are recorded in `research.md`, `data-model.md`, `contracts/`, and `quickstart.md`.

## Technical Context

**Language/Version**: TypeScript 5.x, React 18  
**Primary Dependencies**: Vite 6, React Router 6, Auth0 SPA SDK, `@microsoft/signalr`, existing `fetchJson` HTTP helpers  
**Storage**: N/A (client reads market service APIs; server persistence unchanged)  
**Testing**: Vitest, Testing Library (`@testing-library/react`), `npm run lint` (tsc --noEmit)  
**Target Platform**: Modern browsers; primary demo targets laptop/desktop widths (spec A-004)  
**Project Type**: Web SPA (frontend-only feature slice)  
**UX Consistency Baseline**: Existing app shell (`Header`, `app-content`), CSS variables `--tp-*`, card patterns used by funds and `trading-pets-*` classes in `styles.css`  
**Performance Goals**: PRF-001 orientation within ~10s after load; PRF-002 post-action refresh within a few seconds via existing snapshot + `reloadToken` path; PRF-003 no misleading placeholder balances (see `research.md`)  
**Constraints**: Minimal or zero backend changes (A-002); no new client-only fake outcomes (FR-007); no new calculated fields not from API (A-005)  
**Scale/Scope**: One primary route (`TraderWorkspacePage`) and its four panel areas; avoid unrelated route changes except minor visual consistency if required (FR-008)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-design (initial)

- **Code Quality Gate**: **Pass** — changes scoped to `trading-pets` components and `styles.css`; `npm run lint` remains mandatory.
- **Testing Gate**: **Pass** — plan adds/updates Vitest + Testing Library tests for layout/copy helpers and key workspace assertions (`research.md` §8).
- **UX Consistency Gate**: **Pass** — reuse `--tp-*`, card/grid patterns, and marketplace terminology per spec UX-001–UX-004 (`contracts/workspace-ui.md`).
- **Performance Gate**: **Pass** — PRF-001/PRF-002 mapped to manual demo checklist + optional React Profiler smoke (`research.md` §9).
- **Simplicity Gate**: **Pass** — no new npm dependencies for this feature (`research.md` §10).

### Post-design (Phase 1)

- **Code Quality / Testing / UX / Performance / Simplicity**: **Pass** — `data-model.md` and `contracts/` document consumer API and UI regions without introducing new layers; quickstart defines verification. No constitution violations requiring the complexity table below.

## Project Structure

### Documentation (this feature)

```text
specs/006-pet-trading-ux/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── http-market-api.md
│   └── workspace-ui.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
apps/frontend-spa/
├── src/
│   ├── features/trading-pets/
│   │   ├── TraderWorkspacePage.tsx
│   │   ├── PrimaryMarketPanel.tsx
│   │   ├── SecondaryMarketPanel.tsx
│   │   ├── NotificationsPanel.tsx
│   │   ├── ListingSellerActions.tsx
│   │   ├── MyPetTraderContext.tsx
│   │   ├── tradingPetsApi.ts
│   │   ├── useTradingPetsRealtime.ts
│   │   └── __tests__/
│   ├── styles.css       # .trading-pets-* workspace styles
│   └── App.tsx          # routes (workspace path unchanged)
└── package.json
```

**Structure Decision**: Single SPA under `apps/frontend-spa`; Pet Trading UX work is confined to `features/trading-pets` and shared `styles.css`, consistent with 004-trading-pets implementation layout.

## Complexity Tracking

> No constitution violations required justification for this plan.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
