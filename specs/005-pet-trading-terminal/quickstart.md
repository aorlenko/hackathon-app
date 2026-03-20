# Quickstart: Pet Trading Terminal (005)

## Prerequisites

- .NET 8 SDK
- Node.js version compatible with `apps/frontend-spa`
- SQL Server configured for `market-service`
- Auth0 or the repo's demo auth mode configured the same way as the existing SPA/API setup
- Repository root: `C:\work\my\specit_trading_test`

## Install / restore

1. From the repo root, restore .NET dependencies for `apps/services/TradingPlatform.sln`.
2. Install frontend dependencies for `apps/frontend-spa` if needed.

## Run backend

Start `market-service` using the existing local configuration for connection string, auth, and hub settings.

Expected backend surfaces for this feature:

- HTTP under the existing market-service base URL
- SignalR hub at `/hubs/market`

## Run frontend

```bash
cd apps/frontend-spa
npm run dev
```

Open the SPA URL shown by Vite and sign in through the existing auth flow.

## Primary verification flow

1. Navigate to `/pets/workspace`.
2. Confirm the page loads as a terminal workspace with four regions:
   - market list
   - order book
   - trading panel
   - recent trade feed
3. Select a market entry that has active listings and verify:
   - latest trade price, supply, and trend render in the market list
   - buy-side and sell-side levels render separately in the order book
   - account summary and owned quantity appear in the trading panel
   - the trade feed shows newest trades first
4. Submit:
   - one `Place Bid`
   - one `Place Ask`
   - one `Buy Now`
5. After each action, verify the backend-confirmed result appears on the workspace and no contradictory local state remains visible.

## Realtime verification

1. Keep `/pets/workspace` open in one browser session.
2. Trigger at least one relevant backend change from another session or test harness.
3. Verify the open workspace updates market rows, order book, account summary, and trade feed without a full page reload.
4. Verify new trade rows and changed prices are visually emphasized when the refreshed data arrives.

## Performance check

Use a stopwatch or browser/network timestamps to measure time from backend confirmation to visible UI update for:

- one bid that becomes pending
- one ask placement
- one buy-now trade
- one externally triggered market change while the page stays open

Acceptance target:

- visible update within 3 seconds in at least 90% of observed refresh cycles
- if a cycle misses the target, the workspace still shows the last confirmed backend state instead of speculative data

## Regression check

After terminal verification, confirm these routes still work without required UX changes:

- `/pets/market`
- `/pets/leaderboard`
- `/history/trades`
- `/history/settlements`

## Failure / fallback check

1. Simulate hub loss or stop SignalR delivery.
2. Keep the terminal page open.
3. Verify the page falls back to polling and still converges back to backend state.
4. Re-enable the hub and confirm subscriptions recover without requiring a manual page reload.
