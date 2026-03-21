# Feature Specification: Split trading into Primary supply and Resale marketplace

**Feature Branch**: `008-split-trading-marketplaces`  
**Created**: 2026-03-21  
**Status**: Draft  
**Input**: User description: "Let's split trading into 2 pages. Pet trading should become 'Primary supply market', and new page should be 'Resale marketplace'. With this, we have more real estate on resale marketplace to show your own sells in one column, while having other's offers in second column."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Find and open the right market (Priority: P1)

A trader wants to choose between buying from primary supply and trading on the resale marketplace. They use clear navigation labels so they land on the correct experience without confusion.

**Why this priority**: If users cannot discover or distinguish the two markets, the split fails and support burden increases.

**Independent Test**: Can be verified by navigating the product with only navigation and page titles—each destination is unambiguous.

**Acceptance Scenarios**:

1. **Given** the user is signed in, **When** they open the area for new or first-party supply, **Then** they see a page titled or labeled **Primary supply market** (or equivalent primary-supply wording consistent with the product).
2. **Given** the user is signed in, **When** they open the peer resale experience, **Then** they see a page titled or labeled **Resale marketplace**.
3. **Given** the user is browsing either page, **When** they use global or section navigation, **Then** they can move between **Primary supply market** and **Resale marketplace** in one obvious action each.

---

### User Story 2 - Complete primary supply activities (Priority: P2)

A buyer or participant uses the **Primary supply market** for the same kinds of activities previously grouped under pet trading (discovering and acquiring pets from primary supply, within current product rules).

**Why this priority**: Renaming and isolating primary supply must not remove essential value users already expect from the former single trading page.

**Independent Test**: Can be validated by executing the existing primary-supply user journeys entirely on the **Primary supply market** page.

**Acceptance Scenarios**:

1. **Given** the user is on **Primary supply market**, **When** they perform the primary-supply flows the product already supports (browse, select, and complete acquisition per existing rules), **Then** those flows complete successfully without requiring the resale page.
2. **Given** the user expects “pet trading” wording from prior releases, **When** they look for that capability, **Then** product copy or navigation directs them to **Primary supply market** so they are not lost.

---

### User Story 3 - Manage and compare resale activity in two columns (Priority: P3)

On **Resale marketplace**, a seller sees their own active resale listings in one column and other participants’ buyable resale offers in a second column, giving enough space to scan both without crowding a single list.

**Why this priority**: This is the main UX gain from the split—clear separation of “mine” versus “the market.”

**Independent Test**: Can be validated on **Resale marketplace** alone by creating or viewing listings and confirming column assignment and readability.

**Acceptance Scenarios**:

1. **Given** the signed-in user has at least one active resale listing, **When** they open **Resale marketplace**, **Then** their listings appear in the **your listings** (or equivalent) column.
2. **Given** other participants have resale offers the current user can act on, **When** the user opens **Resale marketplace**, **Then** those offers appear in the **others’ offers** (or equivalent) column, not mixed into the user’s own-listings column.
3. **Given** the user has no active listings, **When** they open **Resale marketplace**, **Then** the own-listings column shows a clear empty state and the others’ offers column still behaves correctly when offers exist or when the market is empty.

### Edge Cases

- User is not signed in: resale columns and actions follow the product’s existing rules for anonymous or signed-out users (view-only, sign-in prompts, or hidden actions—consistent with current trading behavior).
- User has many own listings or the market has many third-party offers: layout remains usable (e.g. scrolling within each column) without collapsing the two-column intent on desktop-sized viewports.
- Narrow viewports (e.g. mobile): the two columns MAY stack vertically while preserving the same grouping (my listings vs others’ offers) and labels.
- Partial failures (e.g. one column’s data unavailable): the available column still renders with an appropriate message; the user can retry or refresh without losing navigation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The product MUST expose **Primary supply market** and **Resale marketplace** as distinct destinations (separate pages or equivalent top-level views), not a single combined trading page.
- **FR-002**: The experience previously referred to as pet trading for **primary supply** MUST live under **Primary supply market** naming (navigation, page title, or primary heading—consistent with product style).
- **FR-003**: **Resale marketplace** MUST present two primary content areas: one for the signed-in user’s own resale listings (sells) and one for other participants’ resale offers available to the user.
- **FR-004**: A user’s own active resale listings MUST NOT appear in the others’ offers column unless the product explicitly defines a special case; default is strict separation.
- **FR-005**: Users MUST be able to reach both markets from predictable navigation entry points without hunting through unrelated screens.
- **FR-006**: Empty, loading, and error states for each column MUST explain what the user is seeing and what they can do next (e.g. create a listing, sign in, try again).
- **FR-007**: Terminology for “your sells / listings” and “others’ offers” MUST remain understandable and consistent with existing trading vocabulary where possible.

### Experience Consistency Requirements

- **UX-001**: User-facing flows MUST reuse established interaction patterns and terminology unless an exception is explicitly approved.
- **UX-002**: Error and empty states MUST provide clear, actionable guidance aligned with existing product voice and accessibility expectations.
- **UX-003**: UI changes MUST document affected components, states, and validation behavior so implementation and review can verify consistency.

### Performance Requirements

- **PRF-001**: On a typical broadband connection, each of **Primary supply market** and **Resale marketplace** MUST show a usable skeleton or first meaningful content within **3 seconds** of navigation under normal load during QA.
- **PRF-002**: Validation MUST be recorded with a repeatable QA checklist (device/network assumptions noted) for both pages’ initial load.
- **PRF-003**: If either page exceeds the budget, the release MUST either include a documented mitigation (e.g. progressive loading) or a scoped exception agreed with product owner before release.

### Key Entities *(include if feature involves data)*

- **Primary supply listing (or offer)**: Represents pets or items offered through the first-party or mint-style supply path; shown on **Primary supply market**.
- **Resale listing (or offer)**: Represents a participant’s listing of a pet (or tradable item) for resale; classified as **mine** (owned by the signed-in user) or **others’** for display on **Resale marketplace**.
- **Market session context**: The signed-in user identity (or anonymous state) that determines which listings count as “mine” versus “others’.”

## Assumptions

- **A-001**: “Primary supply market” corresponds to the product’s existing primary pet acquisition/supply flows; no change to underlying business rules unless a separate feature specifies it.
- **A-002**: “Resale marketplace” uses the product’s existing peer resale listings and bids/offers; the split is primarily information architecture and layout, not a new marketplace concept unless already planned elsewhere.
- **A-003**: Desktop and tablet widths use a two-column layout for resale; smaller screens may stack the two areas while keeping labels.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In moderated usability or QA scripts, **100%** of tasks that require “buy from primary supply” are completed on **Primary supply market** without accidental use of **Resale marketplace** (sample size ≥ 5 sessions or equivalent internal QA sign-off).
- **SC-002**: In the same scripts, **100%** of tasks that require “see my resale listings alongside others’ offers” are completed with both columns identified correctly (sample size ≥ 5 sessions or equivalent internal QA sign-off).
- **SC-003**: **90%** of test participants (or QA reviewers) agree that the resale page has “more space” or “easier scanning” compared to the prior single-page layout, measured by a short post-task survey or structured review rubric.
- **SC-004**: **Zero** critical-severity navigation defects (wrong page, missing entry point, or misleading title) filed against this split in the first release candidate cycle.
- **SC-005**: **0** critical UX consistency deviations in design or QA review against existing trading patterns (labels, actions, empty states).
- **SC-006**: Both pages meet **PRF-001** in **80%** or more of QA runs, or a written exception exists with mitigation.
