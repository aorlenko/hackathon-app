# UI contract: Pet Trading workspace regions

Maps functional requirements to **observable UI structure** for implementation and tests. Routes: `/pets/workspace` (`TraderWorkspacePage`).

## Required regions (FR-002)

1. **Financial summary** (FR-001)  
   - Visible when authoritative snapshot is available.  
   - Shows three labeled metrics: available cash, locked cash, portfolio total.  
   - Must not present placeholder values as real balances (PRF-003).

2. **Primary market**  
   - Distinct section title + short description referencing **new pets**, **limited supply**, **retail** pricing.  
   - Contains breed selection, visible per-breed **remaining supply** and **retail price**, quantity control, primary purchase action (FR-003).

3. **Secondary market**  
   - Distinct section title + description referencing **resale** / **listings** / **bids**.  
   - Listings shown as human-readable cards/rows: pet identity, asking price, actions supported by product (FR-004).

4. **Owned pets**  
   - Distinct section title + description for **inventory**.  
   - One entry per `PetSummaryDto` when `pets.length > 0`; includes breed + stable id + attributes the API provides (FR-005).

5. **Recent activity**  
   - Distinct section (may be titled “Recent activity” with notifications subtitle).  
   - Chronological feed; visually distinguishable event categories; explicit empty state (FR-006).

## Copy and tone (UX-001)

- Prefer: supply, listing, asking price, bid, trade, owned pets.  
- Avoid: framing the page as a generic exchange terminal in titles and primary helper text.

## Cross-cutting (UX-002–UX-004)

- Card-style containment and spacing consistent with `.trading-pets-*` and app shell.  
- Restrained accent use (`--tp-accent` for key figures).  
- Calm error/loading/empty states with next-step hints where applicable.
