---

description: "Task list for Pet Trading Marketplace UX (006)"
---

# Tasks: Pet Trading Marketplace UX

**Input**: Design documents from `C:/work/my/specit_trading_test/specs/006-pet-trading-ux/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Required — `spec.md` mandates user scenarios and testing; `plan.md` / `research.md` specify Vitest + Testing Library in `apps/frontend-spa`.

**Organization**: Tasks are grouped by user story so each increment can be implemented and verified independently (panel-level tests where the UI lives in shared files).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no blocking dependency on incomplete tasks)
- **[Story]**: User story label (US1–US5) for story phases only
- Every description includes at least one concrete file path

## Path Conventions

Primary code: `apps/frontend-spa/src/features/trading-pets/` and `apps/frontend-spa/src/styles.css` per `plan.md`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm SPA toolchain and environment before UX work.

- [x] T001 Run `npm run lint` and `npm test` in `apps/frontend-spa` and note baseline per `specs/006-pet-trading-ux/quickstart.md`
- [x] T002 Verify market API base URL configuration in `apps/frontend-spa/src/config/env.ts` matches backend URL used for manual checks in `specs/006-pet-trading-ux/quickstart.md`
- [x] T003 [P] Cross-check DTO fields used by the workspace in `apps/frontend-spa/src/features/trading-pets/tradingPetsApi.ts` against `specs/006-pet-trading-ux/data-model.md` and `specs/006-pet-trading-ux/contracts/http-market-api.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared presentation helpers and workspace-level loading semantics that all stories rely on.

**⚠️ CRITICAL**: Complete before treating US1–US5 as done on the integrated `/pets/workspace` page.

- [x] T004 [P] Add `formatShortPetId` (or equivalent) and exports in `apps/frontend-spa/src/features/trading-pets/petDisplayUtils.ts` for stable pet identity display per `specs/006-pet-trading-ux/research.md` §4
- [x] T005 [P] Add notification type → label / visual variant mapping in `apps/frontend-spa/src/features/trading-pets/notificationPresentation.ts` per `specs/006-pet-trading-ux/research.md` §6
- [x] T006 Implement snapshot loading and error affordances on the workspace so absent or failed snapshot does not show placeholder currency as real balances (PRF-003) in `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx`
- [x] T007 Extend workspace layout tokens (summary band, grid, card containment) in `apps/frontend-spa/src/styles.css` to match `specs/006-pet-trading-ux/contracts/workspace-ui.md` and `specs/006-pet-trading-ux/research.md` §2

**Checkpoint**: Helpers exist; workspace does not misrepresent financial data during load/error; base styles support the four-region layout.

---

## Phase 3: User Story 1 — Understand the workspace at a glance (Priority: P1) 🎯 MVP

**Goal**: Prominent cash / locked / portfolio summary and four clearly separated, marketplace-labeled areas (primary supply, resale, owned pets, recent activity) per FR-001, FR-002, UX-001.

**Independent Test**: Open `/pets/workspace` with a known snapshot; confirm three summary figures match `GET /api/traders/me/snapshot` and each of the four areas has an obvious marketplace purpose without generic terminal framing (`spec.md` US1).

### Tests for User Story 1

> Write or extend tests first; they should fail until the UI meets the contract in `specs/006-pet-trading-ux/contracts/workspace-ui.md`.

- [x] T008 [P] [US1] Add Testing Library tests asserting labeled financial summary and four distinct regions in `apps/frontend-spa/src/features/trading-pets/__tests__/TraderWorkspacePage.test.tsx`
- [x] T009 [P] [US1] Add tests for marketplace-oriented page title or subtitle copy (not generic exchange framing) in `apps/frontend-spa/src/features/trading-pets/__tests__/TraderWorkspacePage.test.tsx`

### Implementation for User Story 1

- [x] T010 [US1] Implement marketplace page heading and subtitle in `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx` per UX-001 and `specs/006-pet-trading-ux/contracts/workspace-ui.md`
- [x] T011 [US1] Present available cash, locked cash, and portfolio total in a prominent summary band when snapshot is authoritative in `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx` (FR-001)
- [x] T012 [US1] Add explicit section titles and one-line descriptions for primary supply, secondary/resale, owned pets, and recent activity regions in `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx` (FR-002)

**Checkpoint**: US1 acceptance scenarios pass; observer can name all four areas within PRF-001 time on a laptop viewport.

---

## Phase 4: User Story 2 — Buy from primary supply with confidence (Priority: P2)

**Goal**: Primary market shows breed, remaining supply, retail price, quantity, and a clear purchase action distinct from resale (FR-003, US2).

**Independent Test**: Valid purchase updates supply and cash per API; invalid attempts show clear messaging without implying secondary-market actions (`spec.md` US2).

### Tests for User Story 2

- [x] T013 [P] [US2] Add tests for visible retail price, remaining supply, quantity control, and primary purchase control in `apps/frontend-spa/src/features/trading-pets/__tests__/PrimaryMarketPanel.test.tsx`

### Implementation for User Story 2

- [x] T014 [US2] Restructure primary market layout so breed selection, per-breed supply, retail price, quantity, and purchase CTA are adjacent and clearly “new supply” in `apps/frontend-spa/src/features/trading-pets/PrimaryMarketPanel.tsx`
- [x] T015 [US2] Align insufficient cash / zero supply / validation messages with calm, specific copy (UX-003) in `apps/frontend-spa/src/features/trading-pets/PrimaryMarketPanel.tsx`
- [x] T016 [US2] Adjust primary-market spacing, hierarchy, and affordances in `apps/frontend-spa/src/styles.css` targeting classes used by `apps/frontend-spa/src/features/trading-pets/PrimaryMarketPanel.tsx`

**Checkpoint**: US2 scenarios hold; primary flow is visually separate from resale controls.

---

## Phase 5: User Story 3 — Use the resale marketplace in human-readable form (Priority: P3)

**Goal**: Listings read as marketplace offers with pet context, asking price, and existing actions (bid, buy, list, withdraw, accept/reject) per FR-004, US3.

**Independent Test**: Listing cards/rows show pet identity, asking price, and correct actions; copy uses listing/bid/trade language (US3).

### Tests for User Story 3

- [x] T017 [P] [US3] Add tests for listing row/card content (pet identity, asking price, seller context) and presence of action controls in `apps/frontend-spa/src/features/trading-pets/__tests__/SecondaryMarketPanel.test.tsx`

### Implementation for User Story 3

- [x] T018 [US3] Refactor open listings into scannable cards or structured rows using `petDisplayUtils.ts` in `apps/frontend-spa/src/features/trading-pets/SecondaryMarketPanel.tsx` per `specs/006-pet-trading-ux/research.md` §5
- [x] T019 [US3] Update seller/bid action labels and helper text for marketplace terminology in `apps/frontend-spa/src/features/trading-pets/ListingSellerActions.tsx` (UX-001)
- [x] T020 [US3] Add or refine secondary-market card/row styles in `apps/frontend-spa/src/styles.css` for `apps/frontend-spa/src/features/trading-pets/SecondaryMarketPanel.tsx`

**Checkpoint**: US3 acceptance scenarios; no anonymous “order book” feel in headings or primary copy.

---

## Phase 6: User Story 4 — See owned pets as real inventory (Priority: P4)

**Goal**: One entry per `PetSummaryDto` with breed, stable id, and API-provided attributes; listing next steps discoverable (FR-005, US4).

**Independent Test**: Compare UI rows to `snapshot.pets` from `GET /api/traders/me/snapshot`; each pet appears once with consistent fields (`spec.md` US4).

### Tests for User Story 4

- [x] T021 [P] [US4] Add tests for per-pet rows, visible short id via `petDisplayUtils.ts`, and optional attribute fields when present in mock snapshot data in `apps/frontend-spa/src/features/trading-pets/__tests__/TraderWorkspacePage.test.tsx`

### Implementation for User Story 4

- [x] T022 [US4] Refactor the owned-pets block in `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx` to list one row per pet with breed, formatted id, age, health, desirability, intrinsic value, maintenance, and expired flag when provided by DTOs (FR-005, A-005)
- [x] T023 [US4] Improve empty and zero-pets copy in the owned-pets section in `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx` (UX-003, edge cases)
- [x] T024 [US4] Ensure resale list/manage affordance remains discoverable from each pet row via `apps/frontend-spa/src/features/trading-pets/ListingSellerActions.tsx` or inline actions wired from `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx` (US4 scenario 3)

**Checkpoint**: US4 scenarios; no regression to count-only inventory when `pets[]` has multiple entries.

---

## Phase 7: User Story 5 — Scan recent activity quickly (Priority: P5)

**Goal**: Recent activity feed with chronological order, type differentiation, and explicit empty state (FR-006, US5).

**Independent Test**: Trigger distinct notification types; verify ordering and visual distinction; empty feed shows non-error empty state (`spec.md` US5).

### Tests for User Story 5

- [x] T025 [P] [US5] Add tests for descending chronological order, distinct styling per `NotificationDto.type`, and empty-state copy in `apps/frontend-spa/src/features/trading-pets/__tests__/NotificationsPanel.test.tsx`

### Implementation for User Story 5

- [x] T026 [US5] Retitle and subtitle the panel as recent activity/notifications in `apps/frontend-spa/src/features/trading-pets/NotificationsPanel.tsx` per `specs/006-pet-trading-ux/research.md` §6
- [x] T027 [US5] Apply `notificationPresentation.ts` for per-type labels and visual differentiation in `apps/frontend-spa/src/features/trading-pets/NotificationsPanel.tsx` (FR-006)
- [x] T028 [US5] Sort notifications by `createdAt` descending if API order is not guaranteed, and add calm loading/error/empty states in `apps/frontend-spa/src/features/trading-pets/NotificationsPanel.tsx` per `data-model.md` and UX-003

**Checkpoint**: US5 scenarios; activity area remains a fourth distinct region (FR-002).

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Routing, manual validation, accessibility, and quality gates.

- [x] T029 Confirm `/pets/workspace` routing in `apps/frontend-spa/src/App.tsx` remains functionally unchanged; apply only minor shell consistency if required by FR-008
- [x] T030 [P] Execute manual verification steps in `specs/006-pet-trading-ux/quickstart.md` (summary, four areas, owned pets, activity, PRF-001/PRF-002) and fix gaps in `apps/frontend-spa/src/features/trading-pets/` as needed
- [x] T031 [P] Address focus order, contrast, and target sizes for workspace regions in `apps/frontend-spa/src/styles.css` and interactive elements in `apps/frontend-spa/src/features/trading-pets/TraderWorkspacePage.tsx`, `PrimaryMarketPanel.tsx`, `SecondaryMarketPanel.tsx`, and `NotificationsPanel.tsx` (SC-005)
- [x] T032 Run `npm run lint` and `npm test` in `apps/frontend-spa` and resolve regressions introduced by this feature

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No prerequisites
- **Phase 2 (Foundational)**: Depends on Phase 1 — blocks integrated page correctness for PRF-003 and shared helpers
- **Phase 3–7 (US1–US5)**: Depend on Phase 2 for helper modules and workspace load semantics; panel-level work can start after T004–T005 exist where those panels import helpers
- **Phase 8 (Polish)**: Depends on all user stories targeted for the release

### User Story Dependencies

- **US1 (P1)**: After Phase 2 — establishes page shell and four regions for demo orientation (MVP)
- **US2 (P2)**: After Phase 2 — can be tested via `PrimaryMarketPanel.tsx` in isolation; integrated check after US1 layout lands
- **US3 (P3)**: After Phase 2 — uses `petDisplayUtils.ts`; integrated with US1 section headers
- **US4 (P4)**: After Phase 2 — uses `petDisplayUtils.ts`; owned-pets markup lives in `TraderWorkspacePage.tsx`
- **US5 (P5)**: After Phase 2 — uses `notificationPresentation.ts`

No strict US2→US3 ordering; prefer completing US1 first for end-to-end `/pets/workspace` demos.

### Within Each User Story

- Tests (T008–T009, T013, T017, T021, T025) before implementation tasks in that story where feasible
- Implementation tasks in US1 and US4 respect single-file ownership of `TraderWorkspacePage.tsx` — serialize edits to avoid merge conflicts

### Parallel Opportunities

- **Phase 1**: T003 parallel with T001–T002 if different owners (T001–T002 sequential on same machine recommended)
- **Phase 2**: T004 and T005 in parallel (different new files)
- **Per-story tests**: All `[P]` test tasks for a story can run in parallel as separate additions to the same test file if coordinated, or split into additional test files under `__tests__/` to avoid conflicts
- **Phase 8**: T030 and T031 in parallel (manual vs stylesheet/components)

---

## Parallel Example: User Story 1

```bash
# After Phase 2, run US1 tests together (coordinate on __tests__/TraderWorkspacePage.test.tsx):
T008 [US1] apps/frontend-spa/src/features/trading-pets/__tests__/TraderWorkspacePage.test.tsx
T009 [US1] apps/frontend-spa/src/features/trading-pets/__tests__/TraderWorkspacePage.test.tsx
```

---

## Parallel Example: User Story 3

```bash
# Tests first:
T017 [US3] apps/frontend-spa/src/features/trading-pets/__tests__/SecondaryMarketPanel.test.tsx

# Then parallel implementation tracks (same story, different files after test expectations are set):
T018 [US3] apps/frontend-spa/src/features/trading-pets/SecondaryMarketPanel.tsx
T019 [US3] apps/frontend-spa/src/features/trading-pets/ListingSellerActions.tsx
T020 [US3] apps/frontend-spa/src/styles.css
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1 and Phase 2
2. Complete Phase 3 (US1): tests T008–T009, then T010–T012
3. **STOP**: Run `quickstart.md` checks for summary + four areas; demo MVP

### Incremental Delivery

1. Setup + Foundational → shared helpers and safe loading behavior
2. +US1 → marketplace orientation (MVP)
3. +US2 → primary purchase clarity
4. +US3 → resale readability
5. +US4 → inventory depth
6. +US5 → activity scanability
7. Polish → SC-005, lint/test gates

### Parallel Team Strategy

- Developer A: US1 + US4 (same `TraderWorkspacePage.tsx` — serialize or pair)
- Developer B: US2 `PrimaryMarketPanel.tsx`
- Developer C: US3 `SecondaryMarketPanel.tsx` + `ListingSellerActions.tsx`
- Developer D: US5 `NotificationsPanel.tsx` + `notificationPresentation.ts`

---

## Notes

- Do not add mock trading outcomes (FR-007); all numbers and lists must reflect API and existing refresh/`reloadToken` behavior (`useTradingPetsRealtime.ts`, `MyPetTraderContext.tsx`).
- No new npm dependencies for layout (`research.md` §10).
- `[P]` tasks require different files or non-overlapping edits; skip `[P]` when multiple tasks touch `TraderWorkspacePage.tsx` in the same story.
