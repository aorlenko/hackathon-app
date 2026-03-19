# Quickstart: Visible User Funds

## Goal

Verify that authenticated users can always see their available funds, that the value updates after confirmed trades, and that delayed or failed refreshes present clear status feedback.

## Automated Validation

Run the targeted frontend and backend checks for this feature:

```powershell
npm --prefix apps/frontend-spa test
npm --prefix apps/frontend-spa run lint
dotnet test "apps/services/TradingPlatform.sln"
```

Focus the new or updated coverage on:

- header funds visibility for authenticated users
- loading, updating, and unavailable funds states
- market-service funds projection math for buyer and seller trade outcomes
- contract coverage for `GET /api/accounts/me`
- realtime coverage for `FundsUpdated` delivery and client handling

## Manual Verification

1. Start the existing local trading demo stack and open the SPA.
2. Sign in with demo auth or Auth0-backed auth so an account is bootstrapped.
3. Confirm the header shows a clearly labeled available funds value within 2 seconds of the authenticated workspace becoming interactive.
4. Navigate between `Markets`, `Trade history`, and `Settlement history` and confirm the funds display remains visible and easy to locate.
5. Open a protected market detail page and place a trade that should change available funds.
6. Confirm the header funds amount updates automatically within 1 second of the confirmed trade outcome appearing.
7. Execute multiple balance-changing trades in sequence and confirm the latest visible amount keeps up with each confirmed update.
8. Trigger a rejected trade scenario, such as insufficient cash or insufficient holdings, and confirm the visible confirmed amount does not change.
9. Simulate a delayed refresh or reconnect and confirm the last confirmed amount remains visible with an updating indicator.
10. Simulate a temporary account snapshot failure and confirm the UI shows an unavailable/retry state instead of leaving the area blank.

## Expected UX Outcomes

- The funds area is part of the authenticated shell, not hidden inside a single page.
- The amount is readable at a glance and clearly labeled as available funds.
- Status text distinguishes confirmed, updating, and unavailable conditions.
- Retry or wait guidance is present when the latest amount cannot be confirmed.

## Performance Evidence To Capture

- Timed login scenario proving funds appear within the 2-second budget for successful auth flows.
- Timed trade scenario proving confirmed trade-driven updates appear within the 1-second budget.
- Evidence that any delay beyond 5 seconds produces an updating or unavailable state rather than silently presenting stale data as current.
