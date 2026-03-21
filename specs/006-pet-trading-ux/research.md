# Research: Pet Trading Marketplace UX (006)

Consolidated decisions for implementation. All items derived from `spec.md`, the existing `apps/frontend-spa` Pet Trading feature, and constitution constraints.

## 1. Scope and architecture

**Decision**: Treat the Pet Trading workspace as a **presentation-layer redesign** only: `TraderWorkspacePage` and its child panels (`PrimaryMarketPanel`, `SecondaryMarketPanel`, owned-pets section, `NotificationsPanel`), plus shared styles in `apps/frontend-spa/src/styles.css`. Continue using `tradingPetsApi.ts`, `MyPetTraderContext`, and SignalR refresh behavior unchanged unless a spec requirement cannot be met without a minimal API tweak.

**Rationale**: Spec A-001 and FR-007 require the system of record to remain authoritative; the codebase already exposes the needed DTOs and endpoints.

**Alternatives considered**: New composite backend “workspace” endpoint (rejected: violates minimal-server-change preference). Client-side mock data for demo polish (rejected: violates FR-007).

## 2. Information architecture and layout

**Decision**: Keep a **single scrollable workspace** with (1) page title and short marketplace-oriented subtitle, (2) **financial summary band** immediately below (available / locked / portfolio), (3) a **responsive grid** of the four conceptual areas with **explicit section titles and one-line descriptions** that use pet-marketplace language (primary supply, resale listings, owned pets, recent activity). On laptop widths, prioritize summary + section headers visible without scroll where feasible (CSS: summary sticky optional if it does not harm small viewports).

**Rationale**: Matches FR-001, FR-002, PRF-001, and UX-001; aligns with existing `.trading-pets-summary` and `.trading-pets-grid`.

**Alternatives considered**: Tabbed single-area view (rejected: hides the four-way mental model). Dashboard-style dense terminal layout (rejected: conflicts with UX-001).

## 3. Terminology (primary vs secondary)

**Decision**: Rename visible headings/copy where needed so **“Primary market”** clearly means new supply at retail, and **“Secondary market”** means resale listings and bids—never implying primary purchase uses listing controls. Use terms: supply, listing, asking price, bid, trade, owned pets; avoid stock/crypto framing in titles and helper text.

**Rationale**: FR-002, FR-003, UX-001.

**Alternatives considered**: Generic “Market A / Market B” (rejected: fails acceptance tests).

## 4. Owned pets: identity and attributes

**Decision**: Continue **one list row per `PetSummaryDto`**. Surface a **stable short identifier** using the existing `id` (e.g. truncated UUID or last segment) alongside `breedName`, plus existing numeric fields already shown (age, health, desirability, intrinsic value, maintenance, expired flag). Do not compute new valuation fields client-side.

**Rationale**: FR-005 and A-005; `PetSummaryDto` already carries `id` though the current UI omits it.

**Alternatives considered**: Only breed + aggregate count (rejected: violates FR-005). Friendly pet names from API (not in DTO; rejected unless backend adds field later).

## 5. Secondary market presentation

**Decision**: Evolve open listings into **scannable cards or clearly structured rows** within `SecondaryMarketPanel`: breed + pet identity (`petId` short form or `breedName` + id), asking price as “Asking price”, seller context from `sellerDisplayName` / `sellerEmail`, and actions unchanged (list, bid, withdraw, accept/reject via existing components). Preserve existing behavior for crossing bids vs below-ask flows.

**Rationale**: FR-004, User Story 3.

**Alternatives considered**: Table-only order book (rejected: too “terminal”).

## 6. Notifications / activity feed

**Decision**: Retitle area to **“Recent activity”** (or equivalent) with subtitle clarifying notifications. Keep chronological order from API. Add **per-type visual differentiation** (icon or left border / badge) using existing `NotificationDto.type` and the existing `labelForType` mapping; add **empty state** copy when `rows.length === 0` (FR-006, edge cases).

**Rationale**: FR-006, UX-003.

**Alternatives considered**: Merging notifications into secondary panel (rejected: violates four-area separation).

## 7. Loading, error, and empty states

**Decision**: Ensure **no false financial data**: when `snapshot` is null after load failure, summary should not show placeholder zeros as real balances (align with PRF-003—may require explicit loading/error affordance on workspace in addition to provider-level error). Panels that fetch independently already set loading/error; align messaging tone with UX-003 (calm, actionable).

**Rationale**: PRF-003, UX-003, constitution Principle III.

**Alternatives considered**: Skeleton numbers (rejected if they mimic real currency values).

## 8. Testing strategy

**Decision**: Extend **Vitest + Testing Library** coverage: pure helpers (e.g. notification label/type styling map, pet id formatting), and component tests for `TraderWorkspacePage` / panels asserting **presence of four labeled regions**, summary when snapshot provided, and empty states. Run `npm test` and `npm run lint` in `apps/frontend-spa` in CI/local before merge.

**Rationale**: Constitution Principle II; existing `__tests__/tradingPetsRealtime.test.tsx` pattern.

**Alternatives considered**: E2E only (rejected: insufficient for fast regression on copy/structure).

## 9. Performance validation

**Decision**: Map spec **PRF-001** (10s to orient) and **PRF-002** (updates within a few seconds) to **manual demo checklist** on typical laptop + dev backend: time-to-identify four sections after navigation; after purchase/bid, confirm snapshot + `reloadToken` refresh path. Optional: React Profiler smoke on workspace mount if regressions suspected.

**Rationale**: Constitution Principle IV; no new metrics pipeline required for a UX sprint.

**Alternatives considered**: Lighthouse performance budget enforcement (deferred: not critical for internal demo scope).

## 10. Dependencies and simplicity

**Decision**: **No new npm dependencies** for layout/visual polish; use existing CSS variables (`--tp-*`) and patterns from funds/history features where consistent.

**Rationale**: Constitution Principle V.

**Alternatives considered**: Component library import (rejected: unnecessary for card/grid restyle).
