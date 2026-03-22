# Contract: Demo deployment pipeline

This document is the **authoritative inventory** of external interfaces for the hackathon/demo **deploy** path (**§ A–G**) and the **CI** image-build path (**§ H–I**). Implementation MUST keep workflows, scripts, and this file in sync (spec SC-002).

---

## A. Workflow dispatch inputs

Source: `.github/workflows/deploy-hackathon.yml` (job `deploy` on `ubuntu-latest`, `shell: pwsh` for validate/what-if/deploy/smoke steps except endpoint resolution, which uses `bash` with `jq` and Azure CLI).

| Input | Type | Required | Default | Consumed by |
|-------|------|----------|---------|-------------|
| `environmentName` | string | yes | `dev` | Bicep param `environmentName` |
| `location` | string | yes | `eastus` | RG creation + Bicep `location` (via parameters file / overrides) |
| `resourceGroupName` | string | yes | `rg-trading-hackathon-dev` | `az group`, `az deployment group` |
| `frontendImage` | string | no | `""` | Bicep `frontendImage` when non-empty; otherwise workflow resolves `${ACR_LOGIN_SERVER}/frontend-spa:latest` |
| `marketServiceImage` | string | no | `""` | Bicep `marketServiceImage`; otherwise workflow resolves `${ACR_LOGIN_SERVER}/market-service:latest` |
| `tradeServiceImage` | string | no | `""` | Bicep `tradeServiceImage`; otherwise workflow resolves `${ACR_LOGIN_SERVER}/trade-service:latest` |
| `settlementServiceImage` | string | no | `""` | Bicep `settlementServiceImage`; otherwise workflow resolves `${ACR_LOGIN_SERVER}/settlement-service:latest` |
| `runSmoke` | boolean | yes | `true` | Gates smoke step |

Naming contract for Container Apps (endpoint resolution step) must stay aligned with Bicep. **Azure limits app names to 32 characters**, so the template uses short prefixes:

- `fe-${projectName}-${environmentName}`
- `mkt-${projectName}-${environmentName}`
- `trd-${projectName}-${environmentName}`
- `stl-${projectName}-${environmentName}`

where `projectName` is read from `infra/environments/hackathon/parameters.dev.json` at `parameters.projectName.value`.

**Job step order (reference)** — `deploy` job: Checkout repository → Azure login (OIDC) → Validate Bicep templates and hackathon parameters JSON → Ensure resource group exists (idempotent) → ARM what-if → Deploy infrastructure and container apps → Resolve Container Apps FQDNs for smoke checks → optional Run HTTP smoke checks.

---

## B. GitHub Actions permissions

| Permission | Level | Reason |
|------------|-------|--------|
| `contents` | read | Checkout |
| `id-token` | write | OIDC for `azure/login@v2` |

---

## C. GitHub Environment: `hackathon`

Job `deploy` uses `environment: hackathon`. Secrets and variables below are referenced **by that environment** in the workflow.

### Secrets

| Name | Used in workflow |
|------|-------------------|
| `AZURE_CLIENT_ID` | `azure/login` |
| `AZURE_TENANT_ID` | `azure/login` |
| `AZURE_SUBSCRIPTION_ID` | `azure/login` |
| `SQL_ADMIN_PASSWORD` | `env.SQL_ADMIN_PASSWORD` → script param |

### Variables

| Name | Used in workflow |
|------|-------------------|
| `SQL_ADMIN_LOGIN` | `env.SQL_ADMIN_LOGIN` |
| `AUTH0_DOMAIN` | `env.AUTH0_DOMAIN` |
| `AUTH0_AUDIENCE` | `env.AUTH0_AUDIENCE` |
| `AUTH0_CLIENT_ID` | `env.AUTH0_CLIENT_ID` |
| `ACR_LOGIN_SERVER` | Auto-resolve app image tags in `deploy-hackathon.yml` when image inputs are blank |

---

## D. Azure OIDC (external to YAML)

Not stored in the repo; required in Azure AD:

- App registration (service principal) with **federated identity credential** for GitHub Actions.  
- Subject/audience configuration must allow tokens from this repository (and recommended: environment `hackathon`).  
- RBAC assignment for deployment on target scope.

---

## E. Bicep parameter file contract

Primary file: `infra/environments/hackathon/parameters.dev.json`

- MUST remain valid JSON (validated by `validate-bicep.ps1`).  
- Supplies non-secret defaults; secure `sqlAdminPassword` is injected via CLI `--parameters` from environment in CI, not stored in file.

Script `deploy-hackathon.ps1` always passes:

- `@parametersFile`
- `environmentName=<workflow input>`
- `location=<workflow input>`

Optional CLI overrides when env vars are set: `sqlAdminLogin`, `sqlAdminPassword`, `auth0Domain`, `auth0Audience`, `auth0ClientId`, and non-empty image overrides.

---

## F. Smoke check contract

Script: `scripts/infra/smoke-hackathon.ps1`

| Check | URL |
|-------|-----|
| Frontend | `FRONTEND_URL` — HTTP GET, expect 2xx |
| Market API | `{MARKET_API_URL}/health` |
| Trade API | `{TRADE_API_URL}/health` |
| Settlement API | `{SETTLEMENT_API_URL}/health` |

URLs are produced by the workflow step **Resolve container app endpoints** and passed as job `env`.

---

## G. Change control

Any addition of `secrets.*` or `vars.*` in `deploy-hackathon.yml` or new required `env:` keys in infra scripts **requires**:

1. Update this contract.  
2. Update `quickstart.md` checklist.  
3. Cross-check tasks in implementation (spec FR-003, SC-002).

---

## H. CI workflow `ci.yml` — build and push images (repository scope)

Source: `.github/workflows/ci.yml`, job **`build-images`**.

This job does **not** use GitHub Environment `hackathon`. Values are read from **repository** secrets/variables only.

### When the job runs

- **Trigger**: `push` to `main` or `master` (not `pull_request`), after `infra-validate`, `frontend`, and `backend` succeed.  
- **Job-level condition**: `vars.ACR_NAME != '' && vars.ACR_LOGIN_SERVER != ''`. If either is empty, the job is **skipped**.  
- **Docker build context**: **Repository root** (`.`). Service Dockerfiles use paths such as `apps/frontend-spa/...` and `apps/services/<svc>/src/...`; a per-service context breaks `COPY` and yields “file not found” / MSB1009 in CI.

### Repository variables

| Name | Purpose |
|------|---------|
| `ACR_NAME` | Azure Container Registry resource name (short name) for `az acr login --name` |
| `ACR_LOGIN_SERVER` | Registry login server host (e.g. `myregistry.azurecr.io`) for image tags |

### Repository secrets (OIDC)

| Name | Purpose |
|------|---------|
| `AZURE_CLIENT_ID` | Same pattern as deploy; must exist at **repo** scope for this workflow |
| `AZURE_TENANT_ID` | Same |
| `AZURE_SUBSCRIPTION_ID` | Same |

**Federated credential:** A subject scoped to **`environment:hackathon`** does **not** apply to this job. Add a **separate** federated credential (**Entity type: Branch**, name `main` or `master` to match the repo default branch) so the assertion subject is e.g. `repo:ORG/REPO:ref:refs/heads/master`. Without it, `azure/login` fails with **`AADSTS700213`** (no matching federated identity record).

**Azure RBAC:** The service principal needs permission to push to the registry (e.g. **AcrPush** on that ACR).

---

## I. Change control (CI)

Any addition of `secrets.*` or `vars.*` in `ci.yml` for the image job **requires** updating **§ H** and `quickstart.md` section **3**.
