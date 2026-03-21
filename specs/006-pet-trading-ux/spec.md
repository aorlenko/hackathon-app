# Feature Specification: Pet Trading Marketplace UX (Pet Ledger)

**Feature Branch**: `006-pet-trading-ux`  
**Created**: 2026-03-21  
**Status**: Draft  
**Input**: User description: "Improve the frontend experience of Pet Ledger’s Pet Trading area for a hackathon demo: polished, modern, marketplace-style workspace; preserve primary market, secondary market, owned pets, and notifications as distinct concepts; backend remains source of truth with minimal or no server changes; do not genericize into a trading terminal or hide inventory behind aggregates."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Understand the workspace at a glance (Priority: P1)

A participant opens the Pet Trading workspace and immediately sees how much money they can spend, how much is tied up in pending activity, and their overall portfolio position, plus four clearly separated areas: buying new pets from limited supply, the resale marketplace, their own pets, and recent account activity.

**Why this priority**: Without orientation and financial context, every other task feels uncertain; this establishes the marketplace mental model before any transaction.

**Independent Test**: With a configured account that has known cash, locks, and pets, open the Pet Trading workspace and verify the summary matches authoritative account data and each of the four conceptual areas is visibly labeled with an obvious purpose (not generic “market” or “orders” alone).

**Acceptance Scenarios**:

1. **Given** a signed-in participant with known available cash, locked cash, and portfolio total from the system of record, **When** they open the Pet Trading workspace, **Then** those three values appear together in a prominent summary area without requiring scroll on a typical laptop viewport.
2. **Given** the workspace is loaded, **When** the participant scans the main content, **Then** they can identify separate sections for (a) buying from limited breed supply, (b) resale listings and related actions, (c) pets they own, and (d) recent notifications or activity—each with wording that fits a pet marketplace, not a generic exchange terminal.

---

### User Story 2 - Buy from primary supply with confidence (Priority: P2)

A participant selects a breed, sees how many new pets remain at retail price, sees that price clearly, chooses a quantity within limits, and completes a purchase using an obvious primary action.

**Why this priority**: Primary market supply is a core differentiator from resale; the flow must stay obvious and distinct from secondary trading.

**Independent Test**: Attempt a valid multi-pet purchase and an invalid attempt (insufficient cash or exhausted supply); verify messaging and visible supply and cash update per existing system rules.

**Acceptance Scenarios**:

1. **Given** supply remains for a breed and the participant has enough available cash, **When** they select that breed, set a valid quantity, and confirm purchase, **Then** remaining supply and cash figures update to match system behavior and new pets appear in the owned-pets area without mixing this flow with resale listing creation.
2. **Given** a breed’s supply is zero or cash is insufficient, **When** the participant attempts a purchase, **Then** the interface prevents or clearly explains the failure without implying a secondary-market action.
3. **Given** the primary market section is visible, **When** the participant reads labels and prices, **Then** retail or breed price and remaining supply are visible adjacent to breed selection without hunting through unrelated sections.

---

### User Story 3 - Use the resale marketplace in human-readable form (Priority: P3)

A participant browses other traders’ offers as readable marketplace listings (not abstract order rows), places bids or completes buys when the product already supports it, and creates a resale listing when the product already supports it—all framed as pets for sale, not anonymous instruments.

**Why this priority**: Secondary market behavior is already defined; this story ensures the interface reinforces resale and negotiation, not a crypto-style book.

**Independent Test**: Execute one crossing purchase or bid and one below-ask flow if supported; confirm listing cards or rows show pet-relevant identity, ask price, and actions consistent with existing capabilities.

**Acceptance Scenarios**:

1. **Given** active resale listings exist, **When** the participant views the secondary market section, **Then** each offer is presented so a typical user can understand what pet (or pet identity) is for sale, the asking terms, and what they can do next (for example place bid, buy, or accept—only if already supported).
2. **Given** the participant owns a pet they may list, **When** they look at the secondary market or their pet row, **Then** creating or managing a listing is discoverable without leaving the product’s established flows.
3. **Given** bids or asks are shown, **When** the participant reads the screen, **Then** labels and layout use marketplace language (listing, asking price, bid, sold, trade) rather than terminal-only jargon that obscures the pet context.

---

### User Story 4 - See owned pets as real inventory (Priority: P4)

A participant views each owned pet with enough detail at a glance (breed, identifiable reference, and any standard attributes the system already exposes such as age, health, desirability, or intrinsic value) rather than only a single aggregate count.

**Why this priority**: Ownership is emotional and demo-worthy; aggregates alone fail the “what do I own?” question when the backend already provides instances.

**Independent Test**: Compare the workspace list to the authoritative inventory for the active trader; each pet shown should correspond to one owned instance with consistent fields.

**Acceptance Scenarios**:

1. **Given** the participant owns multiple pets, **When** they open the owned-pets section, **Then** they see a list (or equivalent scannable structure) with one entry per pet, not only a total count, unless the system truly exposes only an aggregate for that viewer.
2. **Given** each pet entry, **When** the participant reads it, **Then** breed and a stable short identifier or name from the system are visible, plus any of age, health, desirability, and intrinsic value that the product already supplies for that context.
3. **Given** listing a pet for resale is allowed, **When** the participant views that pet’s entry, **Then** they can find the next step to list or manage resale without guessing.

---

### User Story 5 - Scan recent activity quickly (Priority: P5)

A participant reviews recent events that matter to them (for example bid received, trade completed, listing created or sold, settlement-related notices) in a compact, time-ordered feed with distinguishable entry types.

**Why this priority**: Trust and demo narrative depend on visible consequences of actions.

**Independent Test**: Trigger at least two distinct event types and confirm both appear with clear visual differentiation and correct ordering among recent items.

**Acceptance Scenarios**:

1. **Given** new notifications or activity items exist for this trader, **When** the participant opens the activity area, **Then** recent items appear with enough visual distinction (layout, label, or simple icon treatment) to tell event types apart at a scan.
2. **Given** multiple items, **When** the participant reads them, **Then** ordering is chronological by recency for the feed, consistent with the product’s existing rules.
3. **Given** an empty feed, **When** the participant views it, **Then** they see a clear empty state that does not look like an error.

---

### Edge Cases

- Very long breed names or many pets: layout remains readable (wrapping, scrolling, or compaction within the section) without hiding the fact of multiple distinct pets.
- Zero cash, zero pets, or no listings: each section still communicates its purpose and shows a helpful empty or zero state.
- Participant attempts an action the trading system rejects: message is visible, specific enough to correct the issue, and aligned with existing product messaging where applicable.
- Small screens: summary and four conceptual areas remain reachable (for example via vertical stacking) without collapsing distinct primary vs secondary meaning into one undifferentiated block.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Pet Trading workspace MUST present a top summary that includes available cash, locked cash, and portfolio total for the active trader, using values consistent with the system of record.
- **FR-002**: The workspace MUST visually separate four concerns: primary market (new pets from limited breed supply at retail pricing), secondary market (resale listings and trader-to-trader actions), owned pets, and notifications or recent activity—each section title and introduction MUST make the marketplace role obvious.
- **FR-003**: The primary market section MUST show breed selection, per-breed remaining supply, retail or breed price, quantity selection when supported, and a prominent control to complete purchase, without requiring users to use secondary-market controls for new supply purchases.
- **FR-004**: The secondary market section MUST present listings in a human-readable marketplace form (cards or clearly labeled rows) showing pet identity information supplied by the system, asking price, and available actions that the product already supports (such as bid, immediate buy at or above ask, seller accept or reject, listing creation or withdrawal).
- **FR-005**: The owned-pets section MUST list individual owned pets when the system exposes a per-pet inventory; each entry MUST show breed and a stable identifier and MUST show any of age, health, desirability, and intrinsic value that the system already provides for display. Aggregate counts MAY supplement but MUST NOT replace the per-pet list when per-pet data exists.
- **FR-006**: The notifications or activity section MUST show recent, trader-relevant events in chronological order with scannable differentiation between event categories (for example bid received, trade completed, listing lifecycle events).
- **FR-007**: All interactive flows on this workspace MUST continue to rely on the existing system of record for validation, state changes, and persistence; the interface MUST NOT substitute mock or offline-only simulations for real outcomes.
- **FR-008**: Scope for this feature is the Pet Trading workspace presentation and structure; other areas of the product (such as standalone listings browser, leaderboard, trade history, settlement history) MUST remain functionally unchanged except for minor visual consistency touches if explicitly needed.

### Experience Consistency Requirements

- **UX-001**: Terminology and labels MUST favor pet marketplace language (supply, listing, bid, ask price, trade, owned pets) and MUST NOT present the experience as a generic crypto or stock terminal as the primary frame.
- **UX-002**: Visual hierarchy MUST use spacing, grouping, and card-style containment so a hackathon observer can follow the demo without a guided tour; emphasis colors MUST be restrained and professional.
- **UX-003**: Error, loading, and empty states MUST be explicit and calm, with guidance on what happened and what the user can do next when applicable.
- **UX-004**: Interactive affordances (hover, focus, primary buttons) MUST be subtle and consistent with a polished demo; flashy animation or decorative complexity is out of scope.

### Performance Requirements

- **PRF-001**: On typical demo hardware and network, after the workspace loads, the participant MUST be able to locate summary figures and all four section purposes within ten seconds without cross-navigation to unrelated product areas.
- **PRF-002**: After the system applies a change the user initiated (purchase, bid, listing action), updated numbers and lists relevant to that action MUST appear in the Pet Trading workspace within a few seconds under normal conditions, consistent with existing product refresh behavior—without requiring an unexplained manual full page reload as the only path.
- **PRF-003**: If the workspace uses progressive loading, partially loaded sections MUST show clear loading state and MUST not permanently display placeholder values that could be mistaken for real balances or inventory.

### Key Entities *(include if feature involves data)*

- **Trader workspace summary**: Available cash, locked cash, portfolio total as authorized for the viewer.
- **Primary supply offer**: Per-breed remaining count and retail price for acquiring new pet instances from the issuer.
- **Resale listing**: Another trader’s pet offered at a positive ask with associated marketplace actions.
- **Owned pet instance**: A unique pet in the viewer’s inventory with display attributes supplied by the system.
- **Activity item**: A time-ordered notification relevant to the viewer describing a market or account event.

### Assumptions

- **A-001**: Underlying trading rules, notifications, and data contracts remain as already implemented for Pet Ledger; this specification changes how information is organized and presented, not the business rules (see existing platform specifications for behavioral detail where needed).
- **A-002**: Delivery favors minimal or zero changes outside the Pet Trading workspace; any change to shared platform behavior is acceptable only if the current product cannot meet FR-005 or FR-007 without it, and must be the smallest possible change.
- **A-003**: The global navigation may retain a dark header if present; main workspace styling is card-based and readable on dark or light surfaces as already chosen by the product shell.
- **A-004**: Responsive behavior targets common laptop and desktop demo widths first; narrow mobile layouts may stack sections vertically while preserving the four conceptual boundaries.
- **A-005**: “Intrinsic value” and other computed fields are shown only when the product already supplies them for the Pet Trading context; the workspace MUST NOT invent new calculated fields that are not backed by the same authoritative data as today.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In moderated usability-style walkthroughs with at least three participants unfamiliar with the prior layout, at least eighty percent correctly identify where to buy new pets from supply, where resale happens, where owned pets are listed, and where recent activity appears—without hints—within two minutes of first viewing the Pet Trading workspace.
- **SC-002**: For scripted accounts with known balances and inventory, one hundred percent of summary figures (available cash, locked cash, portfolio total) shown in the workspace match the system of record after load and after each scripted transaction.
- **SC-003**: When the system exposes multiple owned pets, one hundred percent of those pets appear as distinct entries in the owned-pets section during demo checks (no regression to count-only display).
- **SC-004**: Stakeholder demo checklist: observers agree on a binary pass that the interface reads as a “pet marketplace workspace” rather than a generic exchange terminal, while still supporting the same flows as before the redesign.
- **SC-005**: Zero critical accessibility or contrast regressions in the Pet Trading workspace versus the prior version on a standard review checklist (focus order, readable text, interactive targets).
- **SC-006**: Primary purchase and at least one secondary-market action (per existing capabilities) remain completable end-to-end during rehearsal without new manual workarounds or fake data paths introduced solely for the demo.
