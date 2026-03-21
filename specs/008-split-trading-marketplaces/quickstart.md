# Quickstart: verify Split Primary supply / Resale marketplace (008)

## Prerequisites

- Market API and Auth0 (or local dev config) as for the rest of the repo.
- `apps/frontend-spa` env vars pointing at the market API (`src/config/env.ts`).

## Run the SPA

```bash
cd apps/frontend-spa
npm install
npm run dev
```

Sign in with a configured account (see project workspace notes for test credentials if applicable).

## Navigate

1. Open **`/pets/primary-supply`** — expect **Primary supply market** as the main heading and **no** combined resale workspace from the old single page.
2. Open **`/pets/resale`** — expect **Resale marketplace** and **two** listing areas (your listings vs others’ offers).
3. Hit **`/pets/workspace`** — expect redirect to **`/pets/primary-supply`** (if implemented).

## Functional checks (trace to spec)

| ID | Check |
|----|--------|
| FR-001 / US1 | Header (or equivalent) has clear entry points to **both** markets. |
| FR-002 / US2 | Primary purchase flow completes **only** using the primary-supply page. |
| FR-003 / US3 | With your own active listing, it appears **only** in the your-listings column; others’ listings in the second column. |
| FR-004 | No row with `sellerTraderId === your traderId` appears in the others’ column. |
| FR-006 | Empty states when you have no listings / no market; loading and error messages are understandable per column or shared banner. |
| Edge | Resize to narrow width: columns **stack** but labels and grouping remain clear. |

## Performance (PRF-001, SC-006)

**Record**: browser type, approximate network (e.g. office broadband), and **time from navigation start** until first meaningful paint (skeleton, heading + first list row, or equivalent).

- Repeat **at least 5** navigations per route (cold or soft as agreed with QA).
- **Pass** if ≥ **80%** of runs meet **3 seconds** per PRF-001, or document a **scoped exception** with mitigation per PRF-003.

## Quality gates (constitution)

```bash
cd apps/frontend-spa
npm run lint
npm test
```

Include test/UX/performance notes in the PR description.
