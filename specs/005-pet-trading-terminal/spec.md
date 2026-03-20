# Feature Specification: Pet Trading Terminal Experience

**Feature Branch**: `005-pet-trading-terminal`  
**Created**: 2026-03-20  
**Status**: Draft  
**Input**: User description: "Transform the current Pet Trading UI into a backend-driven trading terminal experience with a market view, order book, trading panel, and live trade feed while keeping existing backend logic and other pages intact."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Trade from a single terminal workspace (Priority: P1)

A trader can open the Pet Trading page and see a single trading workspace that combines the market view, current order book, trading controls, and recent trades so they can evaluate the market and act without jumping across multiple pages.

**Why this priority**: This is the core outcome the request is asking for. If the page does not feel like a trading terminal, the feature misses its main user value even if the underlying trading system still works.

**Independent Test**: Open the Pet Trading page with existing market activity and verify that the page alone provides enough information to inspect the market, review the current book, see account context, and submit a trade-related action.

**Acceptance Scenarios**:

1. **Given** backend market, account, and trade data exist, **When** a trader opens the Pet Trading page, **Then** they see a terminal-style layout with a left market view, a center order book, a right trading panel, and a bottom trade feed.
2. **Given** the market view is populated, **When** a trader reviews a market entry, **Then** they can see its name, latest traded price, current supply, and a recent price-direction indicator without opening another page.
3. **Given** the page remains open during market activity, **When** backend data changes, **Then** the visible market, order book, account summary, and trade feed update without requiring a full manual page reload.

---

### User Story 2 - Submit trading actions that stay backend-authoritative (Priority: P2)

A trader can enter price and quantity, place a buy-side or sell-side order, or use a buy-now action from the trading panel, with every outcome determined by the backend and then reflected back into the workspace.

**Why this priority**: The page cannot function as a trading terminal unless the user can act directly from it and trust that the displayed state comes from the same backend that settles trades.

**Independent Test**: From the Pet Trading page, execute a bid, an ask, and a buy-now flow using existing trading capabilities, then verify the order book, trade feed, balance, and owned pets all reconcile to the backend result.

**Acceptance Scenarios**:

1. **Given** a trader has valid inputs and permission to act, **When** they submit `Place Bid`, `Place Ask`, or `Buy Now`, **Then** the action is sent to the existing backend workflow and the page shows the backend-confirmed result rather than a locally simulated outcome.
2. **Given** a submitted action changes the market state, **When** the backend completes the request, **Then** the page refreshes the order book, recent trades, user balance, and owned pets to match the new backend state.
3. **Given** market conditions change before the backend can accept the request, **When** the action is rejected or altered by backend rules, **Then** the trader sees a clear outcome message and the workspace reconciles to current backend state without contradictory local data.

---

### User Story 3 - Monitor market movement in real time (Priority: P3)

A trader can keep the Pet Trading page open and follow recent activity through highlighted new trades, visible price changes, and clear buy-versus-sell visual cues that make the page feel active instead of static.

**Why this priority**: The request explicitly calls for a trading-terminal feel, and that depends on timely market feedback and visual emphasis, not just moving controls into a new layout.

**Independent Test**: Leave the page open while separate trading actions occur and verify that new trades stand out, price changes are visually noticeable, and buy and sell sides remain easy to distinguish at a glance.

**Acceptance Scenarios**:

1. **Given** a new trade is recorded by the backend, **When** it first appears in the trade feed, **Then** the new entry is visually highlighted so traders can spot recent activity immediately.
2. **Given** the latest traded price or best available prices change, **When** the page refreshes to the latest backend state, **Then** the affected values are visually emphasized long enough to be noticed without changing their meaning.
3. **Given** a trader is scanning the page, **When** they compare buy-side and sell-side information, **Then** the interface uses consistent color coding and layout cues that make those sides easy to distinguish in the dark-theme workspace.

---

### Edge Cases

- A selected market entry has no current bids, asks, or recent trades: the workspace must show clear empty states rather than blank or misleading values.
- The trader submits an action using stale visible prices and the backend state changes before completion: the backend outcome prevails, and the page refreshes to the latest confirmed state.
- The trader lacks sufficient balance, inventory, or permissions for the requested action: the request does not create fake local state, and the trader receives a clear explanation.
- Multiple backend updates arrive while the trader is viewing the page: the newest confirmed backend state replaces older visible values, and transient highlights do not leave the page in a misleading state.
- The trader changes the selected market entry while updates are occurring: the workspace keeps the user’s current selection context and repopulates the related panels with the correct backend-backed data.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST redesign only the existing `Pet Trading` page into a trading-terminal-style workspace; other existing pages and workflows MUST remain available and behaviorally unchanged.
- **FR-002**: The `Pet Trading` page MUST present four primary regions in one workspace: market view, order book, trading panel, and recent trade feed.
- **FR-003**: The market view MUST list tradable breeds or items relevant to the current trading experience and show, for each entry, its name, latest traded price, current supply, and a recent trend indicator derived from recent completed trades.
- **FR-004**: The order book MUST show current buy-side and sell-side market interest for the selected market entry using backend-provided state, with bids and asks visually separated and sorted by price.
- **FR-005**: The trading panel MUST allow the trader to enter price and quantity and submit `Place Bid`, `Place Ask`, and `Buy Now` actions from the same page.
- **FR-006**: The trading panel MUST show the trader’s current balance and owned pets using backend-sourced account data.
- **FR-007**: Every trading action initiated from the page MUST be submitted to existing backend-driven workflows; the page MUST NOT simulate trade matching, settlement, or market execution rules locally.
- **FR-008**: After any completed trading action or any backend market change relevant to the page, the system MUST refresh the order book, recent trades, user balance, and owned pets from backend state.
- **FR-009**: The `Buy Now` action MUST execute against the best currently available sell-side opportunity recognized by the backend at submission time and display the backend-confirmed outcome.
- **FR-010**: The recent trade feed MUST show backend-recorded trades in reverse chronological order and visually highlight newly appearing trades.
- **FR-011**: The page MUST visually emphasize meaningful market changes, including newly appeared trades and changed prices, without altering the underlying backend-confirmed values.
- **FR-012**: The `Pet Trading` page MUST use a dark-theme presentation with clear buy-side and sell-side color coding appropriate to a trading-terminal experience.
- **FR-013**: User-facing confirmations, validation failures, and blocked actions MUST reflect backend outcomes and MUST NOT leave users viewing contradictory or speculative local state.
- **FR-014**: The workspace MUST continue to rely on existing backend-provided product interfaces and backend-defined business rules as the single source of truth for displayed market and account data.

### Experience Consistency Requirements

- **UX-001**: The redesigned page MUST use terminology that matches the rest of the trading product so traders are not forced to relearn action labels or market concepts.
- **UX-002**: Dense market information MUST remain scan-friendly, with clear visual hierarchy between market list, order book, action controls, and trade feed.
- **UX-003**: Empty states, blocked states, and action outcomes MUST be explicit and actionable so the page feels trustworthy during fast-changing market activity.

### Performance Requirements

- **PRF-001**: During active use of the `Pet Trading` page, visible changes to the order book, recent trades, balance, and owned pets MUST appear within 3 seconds of the backend confirming the underlying change.
- **PRF-002**: The 3-second responsiveness target MUST be validated through a timed walkthrough that covers at least one bid, one ask, one buy-now action, and one externally triggered market update while the page remains open.
- **PRF-003**: In demo rehearsal, at least 90% of observed refresh cycles for the primary trading workspace MUST meet the responsiveness target in `PRF-001`; if a cycle misses the target, the page MUST preserve the last confirmed backend state rather than inventing an interim result.

### Key Entities *(include if feature involves data)*

- **Market Entry**: A tradable breed or item shown in the market view, including its label, current supply, latest traded price, and recent trend direction.
- **Order Book Snapshot**: The current visible set of buy-side and sell-side price levels for a selected market entry, ordered for trader review.
- **Trading Workspace State**: The combined backend-backed view of selected market entry, order book, trade controls, account summary, and recent trade feed shown on the page.
- **Trade Event**: A completed backend-recorded trade shown in the recent trade feed and used to derive recent market movement indicators.
- **Account Summary**: The trader’s current balance and owned pets as presented in the trading panel.

### Assumptions

- **A-001**: The current backend already exposes or can expose the required market, account, and recent-trade information through existing product-aligned interfaces without changing core business rules.
- **A-002**: `Breed` and `item` language in the request both refer to market entries that the current pet trading domain already understands; the final page may use the established domain terminology as long as the meaning remains clear to users.
- **A-003**: Optional best-bid, best-ask, and last-trade badges may be included if they fit the scope, but they are enhancements rather than required for minimum acceptance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a scripted walkthrough, traders can inspect the market, review the order book, and complete a bid, ask, or buy-now action entirely from the `Pet Trading` page in 100% of test runs.
- **SC-002**: In demo rehearsal, 95% of backend-confirmed market changes appear on the open `Pet Trading` page within 3 seconds, including updates to the order book, trade feed, and account summary.
- **SC-003**: In a verification walkthrough using known backend outcomes, 100% of displayed balances, owned pets, and recorded trade results on the redesigned page match the backend after each completed action.
- **SC-004**: At least 4 out of 5 internal reviewers describe the updated page as feeling closer to a trading terminal than a CRUD-style dashboard after reviewing the primary user flows.
- **SC-005**: 100% of backend-rejected or blocked trade actions produce a clear user-facing outcome and leave no contradictory local market or account state visible on the page.
- **SC-006**: Regression review confirms that Listings, Leaderboard, Trade History, and Settlement History continue to work without required UX changes for this feature.
