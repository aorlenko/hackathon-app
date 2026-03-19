# Demo Runbook

## Goal

Show the full `Order -> Trade -> Settlement` lifecycle with live updates and enough observability to explain system state during the hackathon demo.

## Before Demo Day

1. Confirm the latest `deploy-hackathon` workflow completed successfully.
2. Verify all four Container Apps are healthy and exposed.
3. Verify seeded data includes at least one active market symbol and two demo users.
4. Confirm Auth0 redirect URIs and allowed origins match the frontend URL.
5. Open the Application Insights workbook or saved queries from `docs/observability/lifecycle-dashboard.md`.

## Demo Environment Checklist

- Frontend URL loads without container startup delay.
- `GET /health` succeeds for market, trade, and settlement APIs.
- `GET /api/markets` returns seeded symbols.
- Service Bus topic `trading.lifecycle` has active subscriptions for trade, settlement, and realtime relay.
- SQL database and Key Vault are reachable from the Container Apps environment.

## Recommended Demo Flow

1. Sign in as demo user A in browser window one.
2. Sign in as demo user B in browser window two or an incognito session.
3. Open the same market symbol in both sessions.
4. Submit a buy order from one session and an opposing sell order from the other.
5. Narrate the lifecycle:
   - order accepted in market service
   - trade recorded in trade service
   - settlement started and completed in settlement service
   - live updates reflected in the SPA without refresh
6. Switch to Application Insights to show request health or lifecycle traces.

## Fast Recovery Steps

1. If the frontend is up but data is stale, refresh the market snapshot and verify the SignalR or realtime relay connection.
2. If trade or settlement updates stop, inspect the `trading.lifecycle` topic subscriptions and dead-letter counts.
3. If APIs fail after a deploy, roll back Container App revisions to the previous healthy image tag.
4. If secrets drift, re-run the deployment workflow after updating GitHub environment secrets and variables.

## Known Limits In Current Infra Slice

- Post-deploy smoke validation currently checks anonymous site and `/health` availability only.
- Full scripted login and order placement smoke remains tied to future app automation work in `T057`.
- Local `docker-compose.yml` can now build and run the app containers with `docker compose --profile apps up -d --build`.
