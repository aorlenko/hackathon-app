# SPA routing and navigation contract (008)

Applies to `apps/frontend-spa` React Router configuration and `Header` navigation.

## Public routes (signed-in trading subtree)

| Path | Page title / heading (user-facing) | Primary content |
|------|-----------------------------------|-----------------|
| `/pets/primary-supply` | **Primary supply market** (or equivalent per style guide) | Primary acquisition (`PrimaryMarketPanel`); intro copy about supply; optional workspace refresh banner |
| `/pets/resale` | **Resale marketplace** | Offer-for-sale form; **two columns** — your listings vs others’ offers; bid workflow |

## Legacy / default behavior

| Path | Expected behavior |
|------|-------------------|
| `/pets/workspace` | **Redirect** (replace) to `/pets/primary-supply` — preserves old links and maps “pet trading” home to primary supply (FR-002) |
| `*` (fallback) | Navigate to **`/pets/primary-supply`** (replace), matching updated default landing |

## Navigation (`Header`)

- **MUST** expose **both** destinations with labels consistent with spec (FR-001, FR-005).
- **SHOULD** remove the single combined “Pet trading” entry in favor of the two links above.

## Accessibility

- Each page **MUST** retain a single **`<h1>`** matching the market name.
- Resale page **SHOULD** use **named regions** (`role="region"`, `aria-labelledby`) for “Your listings” and “Others’ offers” columns for screen-reader clarity.

## Out of scope

- `/pets/market` (**Pets for sale**), `/pets/my-pets`, history routes — unchanged unless product requests cross-links copy updates.
