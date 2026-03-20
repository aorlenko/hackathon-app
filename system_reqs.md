System Requirements
1. Business Context / Purpose
Participants will build a “Trading Pets” system to demonstrate end-to-end AI-assisted system development, including design, coding, deployment, and automated testing.

Goal: Human-controlled Traders buy, sell, and manage virtual pets, demonstrating AI-assisted system development rather than trading strategies.

Focus Areas for AI Use:

UI/UX design

Technical architecture / data modeling

Front-end & back-end coding

Cloud deployment (IaC)

Automated testing

2. Core System Mechanics
2.1 Traders
Number: Configurable; the system should support **any number of Traders** (each with their own identity and panel). There is **no** requirement for a fixed count such as three—example flows below may name Traders A, B, and C for clarity only.

UI Panels: Each Trader has a separate panel/window.

Private Information: Inventory, available cash, locked cash, portfolio value, and notifications.

Cash: Fixed initial amount sufficient to buy 5–8 new pets.

Portfolio Value: available cash + locked cash + market value of pets.

2.2 Pets
Dictionary: Read-only, 20 breeds (5 dogs, 5 cats, 5 birds, 5 fish). **Authoritative rows** are tabulated in §4 Ready-to-Use Pet Dictionary below.

Parameters per breed:

Lifespan (years)

Desirability (numeric score)

Maintenance cost

Full health = 100%

Age = 0 (new pets)

Intrinsic Value Formula: Same for all pets; uses breed-specific parameters.

value = basePrice * (health/100) * (desirability/10) * (1-age/lifespan)

image-20260319-153431.png
Supply: Limited (**default 3 new pets remaining per breed/type** unless configured otherwise), decreases as pets are purchased. This **default of 3** is **primary-market supply per breed**, not the number of Traders.

Unique Entities: Each pet instantiated separately; lifecycle tracked individually.

Lifecycle Updates: Age increases continuously; health and desirability ±5% per update. Updates every minute (configurable).

2.3 Market & Trades
New Pet Purchases:

From supply at fixed retail price.

Multiple pets may be purchased if cash and supply allow.

Not considered a secondary-market trade.

Secondary-Market Trades:

One pet per transaction.

Bids may be **below** or **at/above** the listing’s asking price.

**Trading (not auction-by-default):** If a bid is **greater than or equal to** the asking price, the trade **executes immediately**—no separate seller-accept step (buyer “hits the offer”). Settlement is at the **bid amount** (buyer pays that amount; cash is locked from available funds for the attempt, then transferred).

If a bid is **strictly below** the asking price: the **single highest** such bid is active; its cash is **locked**; the **seller may accept or reject**; if accepted (or if a later bid **crosses** the ask), the trade executes immediately. A new **higher** below-ask bid replaces the prior bid and releases the previous bidder’s locked cash.

Bids > available (unlocked) cash are rejected.

Buyers cannot bid on their own pets.

Buyers only see status of their own bids (active, rejected, withdrawn, outbid); **crossing** bids result in a **completed trade**, not a lingering pending bid.

Listings:

Asking price > 0.

Pet listings can be withdrawn by seller; active **pending (below-ask)** bids are rejected and locked cash released.

Only one active listing per pet.

Multiple pets may be listed simultaneously.

2.4 UI & Views
Trader Panel: Inventory, available cash, locked cash, total portfolio value.

Market View:

Current listings

Asking price

Most recent trade price

New supply count

Default order = newest listings first (optional sorting/filtering)

Analysis / Drill-Down View:

Full pet fundamentals (age, health, desirability, intrinsic value)

Expired status

Notifications:

Bid received, accepted, rejected, withdrawn, outbid

Include pet, price, counterparty

Chronological order

2.5 System Behavior
Sequential actions sufficient for demo; no concurrency enforcement required.

Trades and valuation updates trigger immediate UI refresh if any metrics change.

Expired pets remain in inventory; residual market value is market-driven.

Cash for **pending below-ask** bids locked; released upon bid withdrawal, rejection, **supersede by higher below-ask bid**, or **crossing bid** that executes.

Multiple active bids across different pets allowed.

3. Optional / Bonus Features
Sorting/filtering in market view

Bid timestamps for history

Performance / scalability considerations

Audit trails / ledger beyond notifications

Enhanced UI interactions (confirmation prompts, visualization enhancements)

4. Ready-to-Use Pet Dictionary


Intrinsic Value Formula



Where:

Base Value = Suggested retail price for the pet type (set in the dictionary)

Health = Current health percentage (0–100%)

Desirability = Breed-specific desirability (1–10 scale), normalized in the formula by **dividing by 10** (so 10 → factor 1.0).

Age = Current age of the pet in years

Lifespan = Maximum lifespan of the breed in years

**Formula (same for all pets):** `value = basePrice × (health/100) × (desirability/10) × (1 − age/lifespan)`  
(Clamp or define behavior at/after end of lifespan as needed; secondary-market pricing may still apply.)

Notes:

Age starts at 0 when purchased from supply.

Health and desirability fluctuate ±5% each update (once per minute).

Residual value at end-of-life is determined by the market (Traders can still bid on “expired” pets).

### Breed dictionary (twenty breeds)

All new pets from supply start at **Health 100%** and **Age 0**; **Maintenance** is the breed’s ongoing cost metric; **Base/Retail Price** is the breed’s fixed retail price for primary purchase and the **basePrice** input to the intrinsic value formula.

#### Dogs

| Breed | Lifespan (yrs) | Desirability | Maintenance | Health (start) | Age (start) | Base/Retail Price |
|-------|----------------|--------------|-------------|----------------|-------------|-------------------|
| Labrador | 12 | 8 | 5 | 100 | 0 | 100 |
| Beagle | 13 | 7 | 4 | 100 | 0 | 90 |
| Poodle | 14 | 9 | 6 | 100 | 0 | 110 |
| Bulldog | 10 | 6 | 7 | 100 | 0 | 80 |
| Pit Bull | 11 | 5 | 5 | 100 | 0 | 70 |

#### Cats

| Breed | Lifespan (yrs) | Desirability | Maintenance | Health (start) | Age (start) | Base/Retail Price |
|-------|----------------|--------------|-------------|----------------|-------------|-------------------|
| Siamese | 15 | 9 | 4 | 100 | 0 | 90 |
| Persian | 14 | 8 | 6 | 100 | 0 | 85 |
| Maine Coon | 16 | 7 | 5 | 100 | 0 | 80 |
| Bengal | 12 | 6 | 5 | 100 | 0 | 75 |
| Sphynx | 13 | 5 | 7 | 100 | 0 | 70 |

#### Birds

| Breed | Lifespan (yrs) | Desirability | Maintenance | Health (start) | Age (start) | Base/Retail Price |
|-------|----------------|--------------|-------------|----------------|-------------|-------------------|
| Parakeet | 8 | 7 | 3 | 100 | 0 | 25 |
| Canary | 10 | 6 | 2 | 100 | 0 | 20 |
| Cockatiel | 12 | 8 | 3 | 100 | 0 | 30 |
| Macaw | 50 | 9 | 8 | 100 | 0 | 120 |
| Lovebird | 15 | 5 | 3 | 100 | 0 | 15 |

#### Fish

| Breed | Lifespan (yrs) | Desirability | Maintenance | Health (start) | Age (start) | Base/Retail Price |
|-------|----------------|--------------|-------------|----------------|-------------|-------------------|
| Goldfish | 10 | 5 | 2 | 100 | 0 | 5 |
| Betta | 5 | 6 | 1 | 100 | 0 | 6 |
| Guppy | 3 | 4 | 1 | 100 | 0 | 4 |
| Angelfish | 8 | 7 | 2 | 100 | 0 | 8 |
| Clownfish | 6 | 8 | 3 | 100 | 0 | 10 |

Supplementary **worked scenario tables** (sample ages, health, desirability, and resulting intrinsic values) may be used for acceptance testing and hand checks of the formula; they must stay consistent with the formula above (including **desirability ÷ 10**).

5. Example Flows
5.1 Purchasing New Pets
Trader A buys 2 Labradors from new supply at retail price.

Cash decreases, pets added to inventory, lifecycle metrics start ticking.

5.2 Secondary Market Trade
Trader A lists a Poodle for sale at $Y.

**Crossing bid:** Trader B places a bid of $Z **≥** $Y → trade **executes immediately** without Trader A accepting.

**Below-ask bid:** Trader B places a bid of $Z **<** $Y → bid is pending; Trader A may accept or reject; if accepted, trade executes immediately.

Notifications (wording may vary; must include pet, price, counterparty):

- After a **crossing** bid: both parties receive **trade completed** / acceptance-equivalent messages at price $Z.
- After **seller accept** of a below-ask bid: e.g. Trader A: “Bid accepted by Trader B for Poodle at $Z.” / Trader B: “Your bid accepted by Trader A for Poodle at $Z.”

5.3 Bid Withdrawal
Trader C bids $W on a Bengal.

Trader C withdraws bid → cash released.

Trader A notified: “Bid withdrawn by Trader C for Bengal at $W.”

5.4 Valuation Update
Every minutes, intrinsic value recalculated using ±5% variance.

All affected panels refresh automatically.

5.5 Bid Being Outbid
Trader A lists Bulldog at **$100** (asking price).

Trader B bids **$55** (below ask) → active pending bid.

Trader C bids **$60** (still below ask) → replaces B’s bid, B’s cash released.

Notifications:

Trader A: “New highest bid $60 from Trader C.”

Trader B: “Your bid $55 outbid by Trader C.”

Trader C: “Your bid $60 is currently highest.”

5.6 Trader Delisting a Pet
Trader A has Poodle listed with active bid $40 from Trader B.

Trader A withdraws listing → bid rejected, cash released.

Notification: Trader B: “Bid $40 withdrawn by Trader A (listing removed).”

Pet returned to Trader A inventory.

5.7 Reviewing Intrinsic Value
Trader C views analysis for Cocker Spaniel listed by Trader B.

Analysis shows age, health, desirability, maintenance, intrinsic value.

Trader C decides on bid based on intrinsic value.

5.8 Using Leaderboard
Trader A opens leaderboard:

Trader B: Portfolio $500

Trader C: Portfolio $450

Trader A: Portfolio $470

Trader A identifies which traders/pets to target for maximizing portfolio.

 

Clarifying Questions & Answers
1. Trader Behavior & Controls
Q1: Who controls the Traders?
A: Each Trader is controlled by a human participant (human acts as Trader). For demos, one participant may switch between multiple Trader panels; the platform should still support many Traders concurrently.

Q2: Do Traders act simultaneously or turn-by-turn?
A: Traders operate simultaneously, but since one participant is controlling all of them, actions happen one at a time in any order chosen by the participant.

Q3: Does each Trader have a separate interface?
A: Yes. Each Trader has its own window/panel, simulating how multiple people could trade at once.

Q4: Can Traders see each other’s inventory or cash?
A: No. Each Trader sees only their own cash, locked cash (for active bids), and pets.

2. Market Mechanics & Trading Rules
Q5: Can Traders buy multiple pets at once?
A: Yes, both from the new supply and the market, as long as they have enough cash.

Q6: Can multiple bids exist for the same pet?
A: For **below-ask** bids, only the **highest** counts. If a new higher below-ask bid comes in, it replaces the previous one and releases that bidder’s locked cash. A bid **≥ asking price** does not wait in a queue—it **executes immediately** (or fails validation).

Q7: Can a Trader bid on their own pets?
A: No — a Trader cannot buy their own pets.

Q8: Can bids be higher or lower than the asking price?
A: Yes. If the bid is **≥ the asking price**, the trade **executes immediately** (trading-style). If the bid is **below** the asking price, the seller **accepts or rejects** the current highest below-ask bid.

Q9: What happens if a bid is withdrawn?
A: Locked cash is immediately released, and the seller is notified.

Q10: Can pets be relisted for sale?
A: Yes, but only after withdrawing the previous listing. Any active bids are automatically rejected when withdrawn.

Q11: Can a pet be listed multiple times simultaneously?
A: No. Each individual pet can have only one active listing, but a Trader can list multiple different pets at once.

Q12: Are trades instantaneous?
A: Yes. When a bid **meets or beats** the asking price, execution is **immediate**. When a bid is **below** ask, execution is **immediate upon seller accept** (or when a later bid crosses the ask).

Q13: Are there any cash or bid restrictions?
A: Bids cannot exceed a Trader’s available (unlocked) cash. Cash is locked for active bids and released when a bid is withdrawn or rejected.

3. Pets & Valuation
Q14: Are pets unique or generic?
A: Each pet is unique — its own age, health, and intrinsic value tracked individually.

Q15: How is age handled?
A: Age starts at 0 when purchased new and increases continuously, even if the pet is being traded.

Q16: How is health handled?
A: Health fluctuates ±5% at each valuation update.

Q17: How is intrinsic value calculated?
A: Same formula for all pets: `value = basePrice × (health/100) × (desirability/10) × (1 − age/lifespan)` with desirability on a 1–10 scale (hence divide by 10, not 100).

image-20260319-180053.png
Q18: Can expired pets be sold or bought?
A: Yes. Pets remain in inventory at zero intrinsic value, but traders can still bid on them.

Q19: What are the starting Base Prices for pets?
A: Each breed has a fixed retail price (e.g., Labrador = $100, Beagle = $90, Goldfish = $5, etc.)

Q20: Is the pet dictionary editable by participants?
A: No. The dictionary is read-only.

4. UI / Views
Q21: What should the market view show?
A: Current listings, asking price, most recent trade price, and new supply count. Default order = newest listings first.

Q22: What should the analysis view show?
A: Full fundamentals for each pet — age, health, desirability, maintenance, intrinsic value — for Traders to make decisions.

Q23: What does the leaderboard show?
A: Total portfolio value (cash + locked cash + market value). Updates in real-time as trades and valuations change.

Q24: What notifications are required?
A: Bid received, accepted, rejected, withdrawn, outbid. Each includes pet, price, and counterparty.

5. System Behavior
Q25: How often are valuations updated?
A: Every minute (or configurable). Variance ±5% on health/desirability.

Q26: What happens on UI refresh?
A: If any valuation has changed or a trade occurs, all relevant panels update immediately.

Q27: Can pets have multiple active bids?
A: No. Only one **pending below-ask** bid per listing at a time. **Crossing** bids (≥ ask) **execute** instead of queuing.

Q28: Are there limits on inventory or listings?
A: No. Traders can hold unlimited pets as long as they have cash, and list as many pets as they own.

Q29: How are transactions handled for multiple bids or relisted pets?
A: Highest bid replaces previous bids. Relisting requires withdrawing first, which cancels active bids.

 

UX / Experience Considerations left to Participant Decisions
Notes for Participants:

Scoring may consider thoughtful design choices.

The goal is to see participants make intentional UX decisions rather than follow a rigid template.

1. Market View Display
How pets are visually presented (layout, card vs. table, icons, colors).

Optional sorting/filtering (e.g., by type, price, age, health).

Whether to highlight new listings, recently updated valuations, or expired pets.

2. Trader Panel / Inventory
How inventory is organized and displayed (grouping, tabs, list vs. grid).

How available cash, locked cash, and total portfolio value are shown.

Whether to include visual indicators for active bids.

3. Analysis / Drill-Down View
How the detailed pet fundamentals are presented (charts, tables, visual cues).

Optional highlighting of key metrics (e.g., intrinsic value trends, age vs. lifespan).

How easily a Trader can interpret information to decide on a bid.

4. Leaderboard
How total portfolio values are shown (aggregate, per component, color-coded).

Optional inclusion of visual cues for relative performance vs. other Traders.

Placement and prominence of the leaderboard in the Trader’s panel.

5. Notifications
How alerts are displayed (pop-ups, banners, inline messages).

Duration, visibility, or dismissal mechanics for notifications.

Whether multiple notifications are grouped or stacked.

6. Bid / Trade Interaction
How bidding is initiated and confirmed (modal, inline, drag-and-drop).

How bids are visually distinguished (active, rejected, outbid).

Optional UX cues for “locked cash” or unavailable funds.

7. Listing / Delisting Pets
How sellers list pets for sale (form, drag-and-drop, quick-action buttons).

How withdrawn or relisted pets are visually indicated.

Optional prompts or confirmations for critical actions (accept/reject, withdrawal).

8. Valuation Updates
How real-time updates (age, health, intrinsic value) are reflected in the UI.

Optional visual cues for metric changes (animation, color changes, arrows).

9. Multiple Active Panels
Whether participants show all Trader panels at once or allow toggling between Traders.

Optional decision on responsive layout for multiple simultaneous views.

10. Optional UX Enhancements
Any additional usability improvements that enhance clarity or reduce cognitive load (e.g., tooltips, hover info, color-coded risk indicators, highlighting expired pets).