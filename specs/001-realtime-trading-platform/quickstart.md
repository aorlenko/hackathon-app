# Quickstart: Simplified Real-Time Trading Platform

## Prerequisites

- .NET 8 SDK
- Node.js 20+
- Docker Desktop
- Azure CLI
- Auth0 tenant/application configured (SPA + API)

## 1) Configure Environment

Create local `.env`/user-secrets values:

- `AUTH0_DOMAIN`
- `AUTH0_AUDIENCE`
- `AUTH0_CLIENT_ID`
- `ConnectionStrings__Sql`
- `SERVICEBUS_CONNECTION_STRING`
- `APPLICATIONINSIGHTS_CONNECTION_STRING` (optional local)

The services use EF Core migrations, but **they are not applied automatically** on startup. After SQL Server is reachable, run `dotnet ef database update` for each service’s Infrastructure project (see Docker section below) so the `TradingPlatform` database and tables exist.
For real local end-to-end `Order -> Trade -> Settlement`, `SERVICEBUS_CONNECTION_STRING` must point to a real Azure Service Bus namespace. `Azurite` is not sufficient because it emulates Azure Storage, not Azure Service Bus.
When `AUTH0_DOMAIN`, `AUTH0_AUDIENCE`, and `AUTH0_CLIENT_ID` are configured, the frontend uses Auth0 login and the backend validates real JWT bearer tokens. If Auth0 settings are omitted, the app falls back to demo authentication for local development.

## 2) Restore and Build

```powershell
npm install
dotnet restore apps/services/TradingPlatform.sln
dotnet build apps/services/TradingPlatform.sln
cd apps/frontend-spa
npm install
npm run build
```

## 3) Run Locally (Vertical Slice)

Start dependencies and then launch the apps from source:

```powershell
npm run dev:deps
npm run dev
```

Before placing orders, ensure the target Service Bus namespace already has:
- topic `trading.lifecycle`
- subscription `trade-service.on-order-matched`
- subscription `settlement-service.on-trade-recorded`
- subscription `market-service.relay-realtime`

Alternative containerized profile:

```powershell
docker compose --profile apps up -d --build
```

With Docker SQL (port `14333` on the host), create/update the shared `TradingPlatform` database **before** or **after** bringing containers up (SQL must be running):

```powershell
$cs = "Server=localhost,14333;Database=TradingPlatform;User Id=sa;Password=LocalDevPassword123!;TrustServerCertificate=True;"
dotnet ef database update --project apps/services/market-service/src/MarketService.Infrastructure/MarketService.Infrastructure.csproj --startup-project apps/services/market-service/src/MarketService/MarketService.csproj --context MarketDbContext --connection $cs
dotnet ef database update --project apps/services/trade-service/src/TradeService.Infrastructure/TradeService.Infrastructure.csproj --startup-project apps/services/trade-service/src/TradeService/TradeService.csproj --context TradeDbContext --connection $cs
dotnet ef database update --project apps/services/settlement-service/src/SettlementService.Infrastructure/SettlementService.Infrastructure.csproj --startup-project apps/services/settlement-service/src/SettlementService/SettlementService.csproj --context SettlementDbContext --connection $cs
```

Use your `SQL_SA_PASSWORD` if you overrode the compose default.

Expected local URLs:
- Frontend: `http://localhost:5173`
- Market API + Hub: `http://localhost:7001`
- Trade API: `http://localhost:7002`
- Settlement API: `http://localhost:7003`

## 4) Apply Migrations And Seed Demo Data

After migrations (see above), the Market service migration `SeedDemoData` seeds:
- demo users/accounts
- tradable items
- initial holdings/cash balances

When a real Auth0 user completes sign-in, the frontend now calls Market Service to auto-provision a local demo trading account for that `sub` with starter cash and holdings before protected trading views are used.

Then verify `GET /api/markets` returns seeded items.

## 5) Demo Validation Flow

1. Sign in via Auth0.
2. Open market detail page for one symbol in two browser sessions.
3. Place opposing orders from different users.
4. Confirm:
   - order book updates in real time
   - trade appears in recent trades
   - settlement transitions to terminal state
   - rows exist in the `Trades` and `Settlements` tables, not only in `Orders`

For local Auth0 testing, make sure the SPA application in Auth0 allows `http://localhost:5173` in both Allowed Callback URLs and Allowed Logout URLs, because the frontend redirects back to the SPA origin after login and logout.

## 6) Validate Infrastructure

```powershell
npm run infra:validate
npm run compose:config
```

## 7) Deploy to Azure (Hackathon Environment)

```powershell
$env:SQL_ADMIN_LOGIN = "sqladmintrading"
$env:SQL_ADMIN_PASSWORD = "<secure-password>"
$env:AUTH0_DOMAIN = "<tenant>.us.auth0.com"
$env:AUTH0_AUDIENCE = "https://trading-platform-api"
$env:AUTH0_CLIENT_ID = "<spa-client-id>"

az login
az account set --subscription <subscription-id>
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\infra\deploy-hackathon.ps1 `
  -CreateResourceGroup `
  -ResourceGroupName rg-trading-hackathon-dev `
  -Location eastus `
  -EnvironmentName dev `
  -WhatIf

powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\infra\deploy-hackathon.ps1 `
  -ResourceGroupName rg-trading-hackathon-dev `
  -Location eastus `
  -EnvironmentName dev
```

Deploy container images and update ACA revisions via CI/CD pipeline.

The repository also includes a manual GitHub Actions workflow in `.github/workflows/deploy-hackathon.yml` for OIDC-based deployment.

## 8) CI/CD Minimum Checks

- PR validation passes (lint/tests/contracts/bicep build)
- Main branch builds and pushes all app images
- Bicep what-if/deploy succeeds
- Post-deploy smoke test confirms frontend and API health endpoints
