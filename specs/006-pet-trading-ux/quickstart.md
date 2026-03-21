# Quickstart: verify Pet Trading workspace UX (006)

## Prerequisites

- Market API and dependencies running per repo root / platform docs (SQL, Auth0, etc.).
- `apps/frontend-spa` environment variables pointing at the market API (`env` module).

## Run the SPA

```bash
cd apps/frontend-spa
npm install
npm run dev
```

Sign in with a configured demo account (see workspace rules for test credentials if applicable).

## Navigate

Open `/pets/workspace` (default redirect may land here).

## Manual checks (align with spec success criteria)

1. **Summary (SC-002)**  
   Compare available / locked / portfolio to API `GET /api/traders/me/snapshot` (or network tab response). Repeat after a primary purchase.

2. **Four areas (SC-001, SC-004)**  
   Without guidance, confirm you can point to: primary supply purchase, resale listings, owned pets list, recent activity.

3. **Owned pets (SC-003)**  
   With multiple pets in snapshot, confirm one row per pet and visible identifiers/attributes.

4. **Activity (User Story 5)**  
   Trigger two notification types if possible; confirm ordering and visual distinction; empty state when none.

5. **Quality**  
   Run `npm run lint` and `npm test` before PR.

## Performance spot-check (PRF-001 / PRF-002)

After cold load, locate summary + four section purposes within ~10 seconds on a typical laptop. After a trade or purchase, confirm lists and figures update within a few seconds without requiring a full manual reload as the only mechanism.
