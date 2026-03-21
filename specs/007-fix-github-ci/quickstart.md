# Quickstart: Validate CI locally (007-fix-github-ci)

**Goal**: Reproduce the same failures as `.github/workflows/ci.yml` before pushing, and confirm fixes.

## Prerequisites

- Node.js 20+
- Docker with Compose v2 (`docker compose`)
- PowerShell 7+ (`pwsh`)
- Azure CLI (`az`) with Bicep (`az bicep version` works)
- .NET 8 SDK

## Infra job parity (`infra-validate`)

From the **repository root**:

```powershell
npm run compose:config
npm run infra:validate
```

Expect: compose config prints resolved YAML; Bicep build completes (warnings may appear; errors fail the script).

## Frontend job parity (`frontend`)

```powershell
cd apps/frontend-spa
npm ci
npm run lint --if-present
npm test --if-present
npm run build --if-present
cd ../..
```

## Backend job parity (`backend`)

From the **repository root**:

```powershell
dotnet restore apps/services/TradingPlatform.sln
dotnet build apps/services/TradingPlatform.sln --configuration Release --no-restore
dotnet test apps/services/TradingPlatform.sln --configuration Release --no-build
```

## Full monorepo check (optional)

```powershell
npm test
npm run lint
```

(Uses root `package.json` scripts chaining .NET + frontend.)

## GitHub-side confirmation

After changes, open a PR (or use `workflow_dispatch` on `main`) and confirm:

1. Each failing case shows a **specific step name** in the Actions UI.
2. At least five consecutive successful runs on normal activity (SC-001), once fixes land.

**Do not** use this feature as a reason to edit `deploy-hackathon.yml` unless you document a **minimal exception** per FR-003.
