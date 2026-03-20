# Quickstart: Trading Pets demo (004)

## Prerequisites

- .NET 8 SDK, Node.js (match repo), SQL Server (local or Docker) as configured for market-service
- Auth0 (or repo’s demo auth) configured like existing SPA + API
- Repository root: `c:\work\my\specit_trading_test` (adjust for your machine)

## One-time setup

1. Apply EF migrations for market-service after pet schema lands (commands will mirror existing service docs — e.g. `dotnet ef database update` from Infrastructure project).
2. Seed **20 breeds** (values from `system_reqs.md` §4), **primary supply per breed** (config; default often **3**—that is **supply**, not how many Traders), and **N Traders** for any **N** you need for the demo, each with **initial cash** per `research.md` / config.
3. Frontend: `npm install` under `apps/frontend-spa` if not already done.

## Run backend

From solution or service folder (follow existing repo convention):

- Start **market-service** with connection string and Auth0 settings pointing at dev resources.

## Run frontend

```bash
cd apps/frontend-spa
npm run dev
```

Open the SPA URL (e.g. `https://localhost:5173` — per Vite config).

## Demo flows (acceptance mapping)

Execute flows equivalent to **§5.1–5.8** in `system_reqs.md`:

| # | Flow | Spec refs |
|---|------|-----------|
| 5.1 | Purchase new pets from supply | US1, FR-006, FR-009 |
| 5.2 | List → **crossing** bid (≥ ask, instant trade) **or** below-ask → seller accept | US2, FR-014–FR-015 |
| 5.3 | Withdraw **below-ask** bid | US2 |
| 5.4 | Valuation tick updates age/health/desirability/intrinsic | US4, FR-008 |
| 5.5 | Outbid path (below-ask only) | US2 |
| 5.6 | Seller withdraw listing | US2, FR-017 |
| 5.7 | Analysis fundamentals | US3, FR-019 |
| 5.8 | Leaderboard comparison | US3, FR-020 |

## Performance check (PRF-001 / SC-005)

- With browser devtools or a short script, record time from **command success** (HTTP 200) to **visible UI update** for: purchase, bid (**including instant cross**), accept, reject, withdraw bid, withdraw listing, and post-tick portfolio change.
- **Target**: ≤ **5 seconds** in ≥ 90% of trials during rehearsal (SC-005).

## Realtime fallback

- **Primary**: SignalR connection to market hub; subscribe per `contracts/realtime-contracts.md`.
- **Fallback**: If hub disconnected **> 10s**, SPA polls:
  - `GET /api/traders/{activeTraderId}/snapshot` every **5s**
  - `GET /api/traders/{activeTraderId}/notifications` every **5s**
  - `GET /api/market/listings` every **5s** while market view focused  
  Document any deviation in the SPA README if product chooses different intervals.

## Multi-trader demo

- Use **active trader switcher** (session-local) with server-side **authorization** so each API call only succeeds for traders the user may impersonate (demo config).

## Troubleshooting

- **Stale UI**: verify SignalR connection id and subscription; fall back to poll.
- **403 on trader routes**: trader not mapped to user or wrong `traderId` in request.
- **Formula mismatch**: compare API intrinsic to hand calculation using `max(0, 1 − age/lifespan)` and ÷10 on desirability (`spec.md` FR-007).

## PRF-001 / PRF-003 (implementation notes)

- **SignalR**: SPA connects to `VITE_MARKET_HUB_URL` (default `${marketApi}/hubs/market`), subscribes to trading-pets groups, and refetches snapshot on hub events.
- **Fallback polling**: if the hub fails to start, the SPA begins **5s** HTTP polling after **10s** without a connection (see `useTradingPetsRealtime.ts`). Document timed rehearsal results here after facilitator runs §5.x with a stopwatch.
