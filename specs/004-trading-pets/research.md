# Research: 004 Trading Pets Platform

Consolidated decisions for implementation planning. All items that were candidates for “NEEDS CLARIFICATION” in the plan template are resolved here.

---

## R-1 — Where pet domain logic lives

**Decision**: Implement **primary supply, pets, listings, bids, secondary settlement, notifications, and valuation tick** primarily in **market-service** (Option A from `specs/003-trading-pets/plan.md`).

**Rationale**: Single transactional boundary for purchase, bid replacement, accept/reject, **crossing execution**, and listing withdraw; matches FR-022 sequential handling; fits existing hub + account patterns; avoids distributed transactions for a demo scope.

**Alternatives considered**: Split writes to trade-service (Option B) — rejected for this spec because it adds cross-service orchestration without a hard requirement.

---

## R-2 — “Recent trade price” on market view (FR-018)

**Decision**: Show **last completed secondary-market trade price for the pet’s breed** (most recent `TradeExecuted` timestamp for that `BreedId`), **same rule for all viewers**. If no secondary trade exists for that breed, show a **null/“—”** indicator — not intrinsic value.

**Rationale**: Gives comparable “market context” across listings; stable definition for UI and tests; aligns with `system_reqs.md` “most recent trade price” without exposing private bidder state.

**Alternatives considered**: Last trade per pet instance — rejected (too sparse for breed-level market cards). Use intrinsic — rejected (conflicts with distinct “recent trade” field in requirements).

---

## R-3 — Intrinsic value at / after end of lifespan (FR-007, edge cases)

**Decision**: Compute intrinsic using  
`basePrice × (health/100) × (desirability/10) × max(0, 1 − age/lifespan)`  
with **health** clamped to `[0, 100]` and **desirability** (instance field used in formula) clamped to `[1, 10]` after each tick. **Expired** flag: `age >= lifespan` (or `>` if using discrete year steps — document in implementation to match tests). **Secondary bidding** on expired pets allowed; intrinsic may be **0** but UI still shows listing/ask-driven activity.

**Rationale**: Prevents negative factors; keeps formula explainable and hand-checkable (SC-002); matches “residual value market-driven” when intrinsic hits zero.

**Alternatives considered**: Allow negative factor — rejected (breaks SC-002 and user trust). Freeze age at lifespan — rejected (spec says age advances; use max clamp on factor instead).

---

## R-4 — Desirability: breed vs instance (FR-004, FR-008)

**Decision**: **Breed** carries baseline desirability (1–10). Each **Pet** stores **current desirability** for valuation; on each tick, apply **independent ±5%** (relative) to **health** and **current desirability**, then **clamp** health to `[0,100]` and desirability to `[1,10]`.

**Rationale**: Satisfies “desirability score on breed” in dictionary while allowing per-instance drift required by lifecycle updates.

**Alternatives considered**: Mutate breed row — rejected (shared across all pets of breed). Store only deltas — rejected (unnecessary complexity for demo).

---

## R-5 — Realtime vs fallback (PRF-001, PRF-003)

**Decision**: **SignalR** pushes for trader snapshots, market list, leaderboard, notifications, and valuation batches. **Fallback**: if hub disconnected, SPA **polls** trader + notification endpoints on a **fixed interval (e.g. 5s)** with a **single documented behavior** in `quickstart.md`.

**Rationale**: Meets “few seconds” visibility; avoids indefinite staleness.

**Alternatives considered**: Poll-only — rejected (worse demo UX). Longer poll without hub — acceptable only as fallback.

---

## R-6 — Trader identity vs Auth0 user (A-003)

**Decision**: Persist **Trader** as a first-class entity (**configurable count N**, any value the environment chooses—**no** “exactly three Traders” rule). The **default 3** in `system_reqs.md` §2.2 refers to **primary supply per breed**, not to how many Traders exist. Map **Auth0 `sub`** to **at most one Trader** for production-shaped demos; support **session-local “active Trader”** switcher in UI for multi-trader demos from one login by **scoping API calls** to selected `traderId` **only when** the authenticated user is allowed to act as that trader (e.g. demo mode whitelist or shared demo account — product rule in config).

**Rationale**: Satisfies private-by-trader rules while allowing facilitator demos.

**Alternatives considered**: One user = one trader only — rejected (blocks multi-trader single-human demo). Unscoped client-side-only trader — rejected (insecure for real auth).

---

## R-7 — Primary purchase batching (FR-009)

**Decision**: Expose **one HTTP command** accepting **breed id + quantity**; server validates **cash and supply** atomically in one transaction; **mint N pets** with age 0, health 100, desirability = breed baseline.

**Rationale**: Matches spec; simpler than multiple round-trips.

---

## R-8 — Rounding (SC-002)

**Decision**: Use **decimal(18,2)** (or equivalent) for money; intrinsic value rounded to **2 decimal places** for API display and equality tests unless tests specify otherwise; document in API contract.

**Rationale**: Reproducible hand calculations within tolerance.

---

## R-9 — Notification payload identity

**Decision**: Notifications store **event type**, **timestamp**, **pet id + display name**, **amount (bid/trade price)**, **counterparty trader id + display label**, and **listing id** where applicable; ordered **chronological descending** in API (newest first) or ascending with explicit `order` query — **pick one globally** (recommend **newest first** for feed UI with “load more”).

**Rationale**: Satisfies FR-021; keep one consistent ordering in contracts.

---

## R-11 — Bid vs ask: immediate trade vs negotiation

**Decision**: Model secondary market as **trading**, not a pure auction: if `bidAmount >= askingPrice`, **execute the trade in the same request** (pet transfer, settle **bid amount** from buyer to seller, close listing). If `bidAmount < askingPrice`, apply **single highest pending bid**, **lock** cash, **seller accept/reject** only for that pending state. A later **crossing** bid supersedes any pending below-ask bid (release prior lock) and **fills** immediately.

**Rationale**: Matches user requirement for “trading” semantics (hit/lift the offer) while preserving below-ask negotiation and demo flows.

**Alternatives considered**: All bids require seller accept — rejected (user explicitly wants instant execution at/above ask). Continuous double auction — rejected (out of scope).

**Alternatives considered**: Ascending only — acceptable if UI matches; must be fixed in contract tests.
