# Research: Visible User Funds

## Decision 1: Place the primary funds display in the authenticated header

- **Decision**: Show the user's available funds inside the existing authenticated header auth panel so it remains visible across protected trading routes.
- **Rationale**: The current SPA already has a persistent authenticated shell in `App.tsx`, and the auth panel is the only cross-route location guaranteed to stay on screen while the user navigates between market detail, trade history, and settlement history. This satisfies the spec's visibility and consistency requirements without introducing a second top-level summary surface.
- **Alternatives considered**:
  - Add the funds display only to `MarketDetailPage`: rejected because it would disappear on other primary trading screens and fail the "always clear" requirement.
  - Add a new dashboard/banner region above routed content: rejected because it adds layout complexity and duplicates the purpose of the existing header shell.

## Decision 2: Separate account bootstrap from account snapshot refresh

- **Decision**: Keep `POST /api/accounts/me/bootstrap` for initial account provisioning and add a side-effect-free `GET /api/accounts/me` endpoint for reading the latest confirmed funds snapshot.
- **Rationale**: The existing bootstrap endpoint is appropriate for sign-in provisioning, but retry and refresh flows for loading, unavailable, reconnect, and delayed-update states should not depend on a mutation-style endpoint. A dedicated query endpoint lets the frontend refresh confirmed funds cleanly after reconnects, retries, or missed realtime messages.
- **Alternatives considered**:
  - Reuse the bootstrap endpoint for every refresh: rejected because it couples read behavior to account creation/bootstrap semantics and makes retry logic harder to reason about.
  - Store the bootstrap response once and rely only on realtime after login: rejected because the spec requires clear recovery behavior when current funds cannot be retrieved or a refresh is delayed.

## Decision 3: Keep market-service as the funds source of truth

- **Decision**: Extend the existing `DemoAccount` projection in market-service instead of introducing a new account service or separate funds datastore for this feature.
- **Rationale**: The current system already stores `CashAvailable` and holdings in market-service, validates orders against that data, and owns the account bootstrap endpoint. Extending that existing ownership keeps the design simple and preserves current service boundaries while still enabling reliable UI display and updates.
- **Alternatives considered**:
  - Create a new dedicated account/funds service: rejected because it is disproportionate to the feature scope and would add cross-service coordination, contracts, and deployment complexity.
  - Keep funds purely as frontend-only derived state: rejected because confirmed balances need a durable backend source of truth for reconnects, retries, and validation.

## Decision 4: Apply confirmed funds updates on `TradeRecorded`

- **Decision**: Update the demo account projection when market-service consumes `TradeRecorded`, then publish the resulting funds snapshot as a realtime `FundsUpdated` message.
- **Rationale**: `TradeRecorded` is the first confirmed lifecycle event that represents a trade outcome the user can trust. Updating funds at that point aligns with the spec's requirement that confirmed trades change visible funds quickly, while rejected or cancelled trade attempts will not emit `TradeRecorded` and therefore cannot incorrectly affect the displayed amount.
- **Alternatives considered**:
  - Update funds on order submission: rejected because order acceptance does not guarantee a trade occurred and would violate the requirement against showing unconfirmed changes as final.
  - Update funds on settlement completion: rejected because settlement is later than the confirmed trade outcome and would make it harder to meet the 1-second update budget.

## Decision 5: Reuse the existing SignalR hub with a new user-scoped funds event

- **Decision**: Add a `FundsUpdated` message to the existing `/hubs/market` SignalR hub and deliver it through a dedicated user-scoped account group.
- **Rationale**: The frontend already uses a single market hub client with reconnect behavior, snapshot refresh, and realtime event subscriptions. Extending that channel preserves existing operational patterns and avoids extra connection management or polling overhead.
- **Alternatives considered**:
  - Introduce polling for balance refresh: rejected because it is slower, less efficient, and inconsistent with the repo's current live-update architecture.
  - Add a second SignalR hub just for account data: rejected because a new hub adds connection and deployment complexity without clear benefit for this scoped feature.

## Decision 6: Model funds display state explicitly in the frontend

- **Decision**: Track a UI state machine for funds display with `loading`, `confirmed`, `updating`, and `unavailable` states while retaining the last confirmed amount whenever possible.
- **Rationale**: The spec distinguishes between first load, confirmed values, delayed refreshes, and temporarily unavailable data. An explicit state model allows the header to show a prominent amount, a small updating indicator when a fresher value is pending, and actionable retry guidance when a new snapshot cannot be confirmed.
- **Alternatives considered**:
  - Use only a boolean loading flag: rejected because it cannot express the difference between initial load, stale-but-visible data, and unavailable refresh scenarios.
  - Hide the amount whenever refresh is delayed: rejected because the spec explicitly requires the last confirmed amount to remain visible during temporary delays.

## Decision 7: Validate the feature with layered tests and latency checks

- **Decision**: Cover the feature with frontend UI/state tests, backend unit tests for funds math, contract tests for the new account query and realtime payload, and integration scenarios that time login visibility and trade-driven updates.
- **Rationale**: The constitution requires automated correctness checks across unit and boundary layers, and the feature spec includes measurable performance budgets that must be verified rather than assumed.
- **Alternatives considered**:
  - Rely only on manual QA: rejected because it would not satisfy the constitution's testing requirement or provide repeatable latency evidence.
  - Add heavyweight benchmarking infrastructure first: rejected because targeted timed scenarios are enough for this feature's current scope and keep the design simple.
