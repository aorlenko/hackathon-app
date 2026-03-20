# Feature Specification: Trading Pets Platform

**Feature Branch**: `004-trading-pets`  
**Created**: 2026-03-20  
**Status**: Draft  
**Input**: User description: "Trading Pets platform per specs/003-trading-pets/plan.md and system_reqs.md: configurable traders with private panels, unique pet instances with lifecycle and intrinsic value, limited primary supply, secondary market as **trading** (bid ≥ ask executes immediately; bid &lt; ask uses single highest bid + seller accept/reject), notifications, analysis and leaderboard views, immediate UI refresh when values or trades change"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Buy pets and manage a trader’s holdings (Priority: P1)

A human participant acting as one or more Traders can purchase new pets from limited supply, see their own available cash, locked cash, inventory, and total portfolio value in a dedicated trader workspace, without seeing other Traders’ private holdings or balances.

**Why this priority**: Without primary-market purchase and per-trader financial visibility, the product cannot demonstrate the core “own and value pets” loop.

**Independent Test**: Configure at least two Traders; as Trader A, buy pets from supply until blocked by cash or supply; verify only Trader A’s panel reflects new pets and reduced available cash; verify Trader B’s panel is unchanged.

**Acceptance Scenarios**:

1. **Given** a Trader has enough available cash and supply remains for a breed, **When** they purchase one or more pets from new supply at the breed’s fixed retail price, **Then** available cash decreases by the total cost, pets appear in that Trader’s inventory with age starting at zero and full health, and remaining supply for that breed decreases accordingly.
2. **Given** multiple Traders exist, **When** each opens their own panel, **Then** each sees only their own inventory, available cash, locked cash, and portfolio total—not another Trader’s.
3. **Given** a Trader lacks sufficient available cash or supply is exhausted for a breed, **When** they attempt a purchase that would violate those limits, **Then** the action does not complete and the participant receives a clear explanation of why.

---

### User Story 2 - Trade pets on the secondary market (Priority: P2)

A Trader can list an owned pet for sale with a positive asking price. **Trading (not auction-by-default)**: if a buyer’s bid is **greater than or equal to** the listing’s **asking price**, the trade **executes immediately**—no seller accept step. If the bid is **strictly less than** the asking price, the system keeps the **single active highest** below-ask bid, locks the buyer’s cash, and the **seller may accept or reject** that bid; when outbid (still below ask), the prior bidder’s locked funds are released and the new highest bid locks the new bidder’s cash. Buyers may withdraw a **pending** below-ask bid.

**Why this priority**: Secondary-market rules are the main differentiator from a simple catalog and exercise crossing-the-spread execution, locking, listing, and notification behaviors required for the demo.

**Independent Test**: Using two Traders, run through (a) list → bid **at or above** ask → verify **immediate** trade and settlement; (b) list → below-ask bid → outbid → seller accept, verifying cash locks, releases, pet transfer, and seller/buyer outcomes.

**Acceptance Scenarios**:

1. **Given** a Trader owns a pet with no active listing, **When** they create a listing with asking price greater than zero, **Then** the pet appears as offered on the market view subject to visibility rules, and the Trader may hold multiple listings on different pets.
2. **Given** a pet is listed at asking price **P**, **When** a different Trader places a bid with amount **≥ P** and not exceeding their available cash, **Then** the secondary trade **completes immediately** at that bid amount (pet transfers, seller receives payment, buyer’s funds settle, listing ends), **without** requiring seller accept/reject, and both parties receive notifications consistent with a completed trade (pet, price, counterparty).
3. **Given** a pet is listed at asking price **P**, **When** a different Trader places a bid with amount **&lt; P** within their available cash, **Then** that amount is locked, the bid becomes the active highest pending bid, and the seller is notified of the new highest bid.
4. **Given** an active highest **below-ask** bid exists, **When** another Trader places a **higher** bid that is still **&lt; P**, **Then** the previous bidder’s cash is fully released, the new bidder’s cash is locked for the new highest amount, the seller sees the new highest bid, and the outbid participant is notified they were outbid.
5. **Given** an active highest **below-ask** bid exists, **When** another Trader places a bid with amount **≥ P**, **Then** any prior pending bid is superseded (prior locked cash released), the trade **executes immediately** against the crossing bid per scenario 2, and the listing closes.
6. **Given** an active **below-ask** bid from a buyer, **When** the buyer withdraws the bid, **Then** their locked cash is released immediately and the seller is notified of the withdrawal.
7. **Given** an active **below-ask** bid, **When** the seller accepts, **Then** the trade completes immediately: the pet moves to the buyer, cash moves from buyer to seller according to the agreed bid, locks clear appropriately, and both parties receive acceptance notifications naming pet, price, and counterparty.
8. **Given** an active **below-ask** bid, **When** the seller rejects, **Then** the buyer’s locked cash is released and notifications reflect rejection.
9. **Given** a listed pet with an active **below-ask** bid, **When** the seller withdraws the listing, **Then** the active bid is rejected, the bidder’s locked cash is released, the pet returns to the seller’s inventory, and the bidder is notified that the listing was removed.
10. **Given** a Trader attempts to bid on their own listed pet, **When** they submit a bid, **Then** the bid is rejected and no cash is locked for that attempt.

---

### User Story 3 - Understand the market and compare traders (Priority: P3)

A participant can browse current listings (default newest first), see asking price, recent trade price for context, and remaining new supply counts; open an analysis view for fundamentals of a listed (or appropriately visible) pet; and view a leaderboard ranking Traders by total portfolio value.

**Why this priority**: Supports informed bidding and the “competitive table” story without blocking the first two journeys.

**Independent Test**: With known listing and trade history, verify market ordering and displayed fields; open analysis for a pet and confirm all required fundamentals; verify leaderboard ordering matches computed portfolio totals.

**Acceptance Scenarios**:

1. **Given** active listings exist, **When** a participant opens the market view, **Then** they see current listings with asking price, a recent trade price indicator per the product’s defined rule (consistent for all viewers), new supply count per breed, and default ordering shows newest listings first.
2. **Given** a pet is visible in analysis, **When** a Trader opens the analysis view, **Then** they see age, health, desirability, maintenance, intrinsic value, and whether the pet is expired per the lifecycle rules.
3. **Given** multiple Traders with different holdings and cash, **When** a participant opens the leaderboard, **Then** each Trader is shown with total portfolio value equal to available cash plus locked cash plus the market value of owned pets using the product’s stated valuation rule for holdings.

---

### User Story 4 - Stay informed as values and deals change (Priority: P4)

Participants receive chronological notifications for bid received, accepted (including **immediate** trades when a bid **≥** ask), rejected, withdrawn, and outbid events, each referencing pet, price, and counterparty; panels reflecting cash, locks, pets, and portfolio refresh promptly when a trade or scheduled valuation update changes relevant numbers.

**Why this priority**: Completes the realtime demo narrative and validates that lifecycle and social trading events are observable.

**Independent Test**: Trigger each notification type through scripted flows; run a valuation cycle and confirm affected panels update without requiring a manual full reload.

**Acceptance Scenarios**:

1. **Given** notification-worthy events occur, **When** a Trader views their notification feed, **Then** entries appear in chronological order with the required event types and details (pet, price, counterparty).
2. **Given** a valuation cycle runs on the configured schedule, **When** health and desirability adjust within the defined variance and age advances, **Then** intrinsic values recalculate for affected pets and any panel that shows those values updates for viewers who are allowed to see them.
3. **Given** a trade or bid-state change affects a Trader’s cash or inventory, **When** the change completes, **Then** that Trader’s workspace reflects the new state within the responsiveness expectations in Performance Requirements.

---

### Edge Cases

- Purchase or bid is attempted with insufficient available (unlocked) cash: action fails; no partial lock beyond defined rules.
- Supply for a breed reaches zero: further primary purchases for that breed are blocked until supply is replenished by configuration or data setup (if ever).
- Only one active listing per pet: attempting a second concurrent listing for the same pet is rejected; relisting after withdrawal is allowed.
- Only one active **pending** (below-ask) bid per listing at a time: a new **higher** below-ask bid replaces the prior; no stack of competing simultaneous below-ask bids. A bid **≥ ask** does not sit pending—it **executes or fails** in the same action.
- Pet reaches or passes lifespan: pet remains in inventory; intrinsic value follows the documented formula behavior at end of life; secondary market bidding may still occur, with residual value driven by market activity.
- Many Traders configured: each retains a separate panel identity; sequential consistency per action is sufficient—no requirement for advanced concurrent arbitration beyond documented cash and listing rules.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST support a **configurable number of Traders** (any count the deployment chooses—there is **no** fixed or implied requirement such as exactly three). Each Trader MUST have a distinct identity and dedicated panel or workspace area. Example narratives that name “Trader A, B, C” in `system_reqs.md` are **illustrative** only.
- **FR-002**: Each Trader MUST have a fixed initial cash allocation sufficient to purchase roughly five to eight new pets at typical retail prices (exact amount configurable).
- **FR-003**: The system MUST maintain, per Trader, available cash, cash locked for active bids, private inventory, and a portfolio total defined as available cash plus locked cash plus the market value of owned pets.
- **FR-004**: The system MUST provide a read-only breed dictionary describing exactly twenty breeds (five dogs, five cats, five birds, five fish), each with lifespan, desirability score on a 1–10 scale, maintenance cost, and fixed retail (base) price; participants MUST NOT edit this dictionary through the product UI.
- **FR-005**: The system MUST instantiate each acquired pet as a unique entity with its own lifecycle state (age, health, desirability used in valuation, ownership).
- **FR-006**: Primary supply MUST be limited per breed with a default quantity per breed; purchasing from supply decreases remaining supply and is NOT classified as a secondary-market trade.
- **FR-007**: The system MUST compute intrinsic value for every pet using: base price × (health ÷ 100) × (desirability ÷ 10) × (1 − age ÷ lifespan), with explicit, documented clamping or end-of-life handling so results are consistent and explainable.
- **FR-008**: On a configurable schedule (default: every minute), the system MUST advance pet age and apply independent ±5% variance to health and to desirability (within documented bounds), then recompute intrinsic values and persist updated state.
- **FR-009**: Traders MUST be able to purchase multiple pets in one primary transaction when cash and supply allow.
- **FR-010**: Secondary-market interactions MUST handle one pet per transaction; asking price MUST be greater than zero for an active listing.
- **FR-011**: At most one active listing per pet MUST exist; a seller MUST withdraw an existing listing before creating another for the same pet.
- **FR-012**: For bids **strictly below** the asking price, at most one **active pending** bid per listing MUST exist; that bid MUST be the **highest** such bid received; a new higher below-ask bid MUST replace the prior and MUST release the previous bidder’s locked cash.
- **FR-013**: Bids MUST NOT exceed the bidder’s available (unlocked) cash at submission time; bids from the pet’s owner MUST be rejected.
- **FR-014**: **Crossing bid (trading)**: When a submitted bid amount is **greater than or equal to** the listing’s asking price, the system MUST **immediately** complete the secondary trade: transfer the pet to the buyer, settle **cash equal to the bid amount** from the buyer to the seller (after locking that amount from available cash for the attempt), close the listing, and clear locks—**without** requiring seller accept/reject for that bid.
- **FR-015**: **Below-ask negotiation**: When the active bid is **strictly below** the asking price, the seller MUST be able to **accept** or **reject** that bid; acceptance MUST execute the trade immediately at the bid amount (pet and cash transfer, locks resolved); rejection MUST release the bidder’s locked cash.
- **FR-016**: Buyers MUST see status only for their own bids (e.g., active, rejected, withdrawn, outbid); for **instant** crossing trades (FR-014), the outcome MUST appear as a completed trade / acceptance-equivalent status, not a dangling pending bid.
- **FR-017**: Withdrawing a listing MUST reject any active **below-ask** bid, release locked bidder cash, return the pet to the seller’s inventory, and generate the required notifications.
- **FR-018**: The market view MUST show current listings, asking price, most recent trade price (per the product’s stated rule), and remaining new supply counts, with default sort newest listings first.
- **FR-019**: The analysis view MUST expose full fundamentals and expired status for pets the viewer is permitted to analyze.
- **FR-020**: The leaderboard MUST rank Traders by total portfolio value and update as trades and valuations change.
- **FR-021**: Notifications MUST cover bid received, accepted (including **immediate** cross trades per FR-014), rejected, withdrawn, and outbid, each including pet, price, and counterparty, in chronological order.
- **FR-022**: For demo purposes, sequential handling of actions is sufficient; the system MUST NOT claim stronger concurrency guarantees than this specification.

### Experience Consistency Requirements

- **UX-001**: Trader-facing flows MUST use consistent labels for cash states (available vs locked), listing state, and bid state across panels, market, and notifications.
- **UX-002**: When an action is blocked (insufficient cash, self-bid, duplicate listing, etc.), the participant MUST see a clear, actionable message—not a silent failure.
- **UX-003**: Layout choices (cards vs tables, single vs multiple panels on screen) are left to implementation, but the same information required by this spec MUST be discoverable without conflicting definitions between views.

### Performance Requirements

- **PRF-001**: Under normal demo conditions, after a participant completes a purchase, bid (including **immediate** cross trade), acceptance, rejection, withdrawal, or valuation tick, any numeric or inventory change that the spec requires them to see MUST become visible on their relevant panels within a few seconds while they continue using the trading workspace normally (no extra manual refresh steps unless documented as the supported fallback).
- **PRF-002**: Responsiveness MUST be verifiable through a timed demo script covering the primary flows in section 5 of the authoritative requirements document referenced in planning materials.
- **PRF-003**: If updates cannot be pushed immediately, the product MUST define a single documented fallback (for example periodic refresh) and MUST NOT leave participants viewing stale cash or inventory indefinitely during active trading.

### Key Entities *(include if feature involves data)*

- **Trader**: Human-directed identity with private cash (available and locked), private inventory of pets, notification feed, and computed portfolio total.
- **Breed**: Catalog entry from the read-only dictionary; defines retail price, lifespan, baseline desirability, maintenance; ties to limited primary supply counts.
- **Pet**: Unique instance; references a breed; has owner, age, health, current desirability for valuation, lifecycle status including expired flag; intrinsic value derived from the standard formula.
- **Supply**: Per-breed remaining count for primary market purchases.
- **Listing**: Seller’s active offer of one pet at a positive asking price; at most one active per pet.
- **Bid**: For **below-ask** offers, the single active highest pending bid on a listing; locks buyer cash until replaced, withdrawn, rejected, accepted, voided by listing withdrawal, or **superseded by a crossing bid** that executes immediately (FR-014).
- **Trade (secondary)**: Completed transfer of a pet and settlement of cash resulting from seller acceptance of the active bid.
- **Notification**: Time-ordered message to a Trader about market or bid events with required attributes.

### Assumptions

- **A-001**: “Market value” of pets in portfolio and on the leaderboard uses each pet’s intrinsic value computed by the standard formula (not last trade price), unless a future change explicitly documents otherwise.
- **A-002**: The read-only breed dictionary SHOULD match the **Ready-to-Use Pet Dictionary** in `system_reqs.md` §4 (exactly twenty breeds, five each dogs/cats/birds/fish, with lifespan, desirability (1–10), maintenance, and retail/base price). New pets from supply start at **age zero** and **full health (100%)** as described there. Implementations MAY load that table from seed data or config files derived from the same authoritative values.
- **A-003**: For demos, one human may switch between Traders (session-local “active trader”) or multiple humans may each use one Trader; both patterns MUST remain compatible with private-by-Trader data rules.
- **A-004**: Optional sorting, filtering, bid timestamps, audit ledger, and rich confirmation UX are out of scope for the minimum specification but MAY be added as enhancements without contradicting required behaviors.
- **A-005**: **Primary market supply** (e.g. default remaining count **per breed** on first sale) is a **separate** setting from **Trader count** per `system_reqs.md` §2.2 (“default 3 per type” refers to **supply**, not how many Traders exist). Do not conflate the two in configuration or documentation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A facilitator can demonstrate flows equivalent to sections 5.1 through 5.8 of the authoritative “Trading Pets” requirements (purchase, secondary trade, bid withdrawal, valuation refresh, outbid, seller delist, analysis review, leaderboard comparison) end-to-end with configurable Trader count.
- **SC-002**: For a sample set of pets with known inputs, intrinsic values reported in the product match hand calculations using the published formula (including ÷10 on desirability) within documented rounding tolerance.
- **SC-003**: In a multi-Trader demo, one hundred percent of private fields (inventory, cash, locked cash) remain visible only to the owning Trader across all required views.
- **SC-004**: For every notification type named in FR-021, at least one scripted scenario produces that notification with pet, price, and counterparty present, verifiable in the feed order (including an **immediate cross** trade mapped to the acceptance / completed-trade pattern).
- **SC-005**: After any successful trade or scheduled valuation update, observers see updated portfolio-related numbers on affected Traders’ panels within the time bound described in PRF-001 in at least ninety percent of trials during demo rehearsal.
- **SC-006**: Stakeholders rate the demo as sufficient to showcase AI-assisted design, build, and test of a non-trivial domain (survey or review checklist—binary pass/fail acceptable for internal use).
