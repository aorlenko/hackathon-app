# Research: 005 Pet Trading Terminal

Consolidated planning decisions for the terminal redesign. All technical-context unknowns from the plan are resolved here so Phase 1 artifacts can define contracts and tasks without placeholder assumptions.

---

## R-1 — Scope of the UI redesign

**Decision**: Redesign only the existing `/pets/workspace` route and keep the route, provider, authentication flow, and sibling pet-trading pages unchanged.

**Rationale**: `App.tsx` already isolates the Pet Trading workspace behind `MyPetTraderProvider`, while Listings, Leaderboard, Trade History, and Settlement History are separate routes. This matches `FR-001` and `SC-006` while minimizing regression risk.

**Alternatives considered**: Add a new `/pets/terminal` route or replace other pet pages with terminal views. Rejected because the spec limits scope to the existing `Pet Trading` page and requires other pages to remain behaviorally unchanged.

---

## R-2 — Backend source of truth for the terminal

**Decision**: Add a terminal-specific API surface inside `market-service` that exposes breed-level market rows, a selected-market workspace snapshot, and terminal trading commands while reusing the existing pet-trading domain entities, persistence, and settlement rules.

**Rationale**: The current SPA can fetch breeds, listings, trader snapshots, and notifications, but it does not expose a backend-authoritative recent trade feed or a breed-level order book. A focused terminal API keeps the backend authoritative, avoids client-side stitching of domain logic, and supports the terminal layout without changing other consumers.

**Alternatives considered**: Compose the terminal entirely from existing client calls. Rejected because `FR-004`, `FR-008`, `FR-009`, and `FR-010` require read models and command semantics that are broader than the current listing-centric UI.

---

## R-3 — Order book definition for the selected market entry

**Decision**: Model the terminal order book as a breed-level read projection:

- Ask side = active listings for the selected breed, grouped by asking price, sorted price ascending then oldest listing first.
- Bid side = active pending below-ask bids for listings of the selected breed, grouped by bid amount, sorted price descending then oldest bid first.
- Each level exposes `price`, `quantity`, `orderCount`, and a lightweight recency marker for UI highlighting.

**Rationale**: This gives a true backend-derived buy-side and sell-side view without replacing the existing per-pet listing/bid rules. The projection is additive and can be derived from current listings, bids, and trades.

**Alternatives considered**: Show only sell-side listings and call that an order book. Rejected because `FR-004` explicitly requires buy-side and sell-side interest. Introduce a brand-new matching engine. Rejected because the feature must keep current backend logic authoritative.

---

## R-4 — Terminal trading actions and quantity semantics

**Decision**: The terminal exposes three backend commands, each mapped onto current pet-trading workflows:

- `Place Ask`: for the selected breed, atomically list `quantity` eligible owned pets at the requested ask price, using a deterministic backend selection order for pets not already listed.
- `Place Bid`: for the selected breed, consume compatible asks at or below the submitted price first; if quantity remains and the request price is below remaining asks, create pending per-listing bids against the cheapest remaining listings up to the requested quantity.
- `Buy Now`: server-side convenience command that atomically buys `quantity` pets from the best currently available asks for the selected breed, ordered lowest ask then oldest listing.

**Rationale**: The page needs price-plus-quantity actions that feel like terminal actions, but settlement and listing rules must still be enforced by the backend. A terminal command layer can fan out to existing listing/bid/trade logic without moving execution into the client.

**Alternatives considered**: Force the SPA to loop through existing single-listing commands. Rejected because it would create speculative client behavior, race conditions, and partial-state risks that conflict with `FR-007`, `FR-013`, and `FR-014`.

---

## R-5 — Partial fill and rejection behavior

**Decision**: Minimum-scope terminal commands are backend-atomic with explicit outcome messaging:

- `Place Ask` rejects when the trader does not have enough eligible pets of the selected breed.
- `Buy Now` rejects when the requested quantity cannot be satisfied from currently available asks at submission time.
- `Place Bid` may return a mixed outcome: immediately filled quantity, newly pending quantity, or a full rejection when no valid target listings remain.

**Rationale**: This preserves trust in backend-confirmed state while allowing the terminal to reflect real market movement. Mixed bid outcomes are necessary because a single request can legitimately span both immediate fills and pending below-ask bids.

**Alternatives considered**: Permit broad client-side optimistic partial fill handling. Rejected because the spec requires the backend-confirmed result to prevail and prohibits contradictory local state.

---

## R-6 — Recent trade feed and trend indicator

**Decision**: The bottom trade feed shows completed secondary-market trades for the selected breed in reverse chronological order, and the market list trend indicator is derived from the last two completed trades for that breed when both exist.

**Rationale**: This keeps the terminal centered on the currently selected market entry, gives a clear basis for `FR-003`, `FR-010`, and `FR-011`, and avoids mixing unrelated breeds into the user’s decision context.

**Alternatives considered**: Show a global cross-breed trade tape. Rejected for minimum scope because it weakens the selected-market focus and adds noise to the workspace.

---

## R-7 — Realtime delivery strategy

**Decision**: Keep SignalR as an invalidation channel and HTTP terminal endpoints as the rendered source of truth. The workspace listens to the existing trading-pets hub subscriptions and refetches terminal market rows, the selected workspace snapshot, and the active trader snapshot when relevant events arrive.

**Rationale**: The repo already uses lightweight SignalR invalidation plus refetch for trading pets. Reusing that pattern keeps contracts simple and avoids drift between push payloads and HTTP responses.

**Alternatives considered**: Push full order-book and trade-feed payloads over SignalR. Rejected for minimum scope because it duplicates projection logic and increases contract complexity without being necessary to meet the responsiveness target.

---

## R-8 — Polling fallback to satisfy the 3-second budget

**Decision**: Tighten the workspace fallback from the current generic 5-second polling approach to a terminal-specific fallback that starts after a short disconnect window and polls the terminal endpoints on a 2-second interval until SignalR recovers.

**Rationale**: The existing `useTradingPetsRealtime` fallback (`5s` poll after `10s` disconnect) is too slow for `PRF-001`. A tighter workspace-only fallback keeps the terminal within the 3-second responsiveness target without changing non-terminal pages.

**Alternatives considered**: Keep the current 5-second fallback and rely entirely on SignalR for the performance budget. Rejected because `PRF-001` and `PRF-003` require a credible degraded-mode path.

---

## R-9 — Visual highlighting without speculative state

**Decision**: All values remain backend-derived; the SPA adds short-lived highlight state only by diffing the last confirmed workspace payload against the newly fetched payload:

- New trade rows flash as newly appeared.
- Price cells flash up/down when the backend-confirmed price changes.
- The page keeps the last confirmed payload visible when a refresh fails.

**Rationale**: This satisfies `FR-011` and `PRF-003` while preserving trustworthiness under rapid updates.

**Alternatives considered**: Optimistically insert local trades or price moves before the backend confirms them. Rejected because it violates `FR-007` and `FR-013`.

---

## R-10 — Test strategy for regression-proof delivery

**Decision**: Validate the terminal with:

- Backend unit tests for terminal command allocation and order-book aggregation.
- Backend contract tests for terminal endpoints and unchanged existing listing/bid endpoints.
- Backend integration tests for realtime invalidation and multi-step action reconciliation.
- Frontend Vitest/RTL coverage for terminal route rendering, selection changes, action submissions, diff-based highlighting, and polling fallback.
- Route smoke tests proving Listings, Leaderboard, Trade History, and Settlement History still work.

**Rationale**: The feature changes a user-facing page and introduces new terminal projections, so both new contracts and regression coverage are required by the constitution.

**Alternatives considered**: Rely on manual demo verification only. Rejected because the constitution requires automated tests for correctness and cross-boundary behavior.
