# Quickstart: Demo deployment (hackathon)

Audience: **platform operators** with admin access to the GitHub repo and to an Azure subscription. Complete prerequisites once per org/repo; then trigger deploys as needed.

---

## 1. Prerequisites — Azure (ordered)

Complete these in order before configuring GitHub.

1. **Subscription**  
   - Identify the subscription where demo resources are allowed and copy the **subscription ID**.

2. **Resource group (target)**  
   - Either create an empty resource group in the target region, or rely on the workflow: it runs **`az group create`** with your `resourceGroupName` input (default e.g. `rg-trading-hackathon-dev`).  
   - **`az group create` is idempotent**: if the group already exists, the command succeeds and does not delete existing resources.

3. **Azure AD app registration (OIDC trust for GitHub)**  
   - In Microsoft Entra ID, create an **app registration** (or reuse an automation service principal).  
   - Under **Certificates & secrets** → **Federated credentials**, add a credential for GitHub Actions:  
     - Issuer: `https://token.actions.githubusercontent.com`  
     - Subject: recommended pattern **`repo:ORG/REPO:environment:hackathon`** so only the GitHub Environment `hackathon` can obtain tokens (aligns with `environment: hackathon` in the workflow). Your org may require entity-type **Environment** and the environment name **hackathon**.  
   - Record **Application (client) ID** and **Directory (tenant) ID**.

4. **RBAC for the deployment identity**  
   - Assign the app’s service principal a role that can deploy ARM resources to the target scope (e.g. **Contributor** on the resource group — tighten per org policy).  
   - Without this, deployments fail with **Authorization failed** / HTTP 403 in Azure CLI logs.

---

## 2. Prerequisites — GitHub (ordered)

After Azure setup, configure the repository.

1. **Environment**  
   - Create a GitHub Environment named **`hackathon`** (must match `environment: hackathon` in `.github/workflows/deploy-hackathon.yml`).  
   - Store the secrets and variables below **on that environment** so they apply only to deploy jobs.

2. **Secrets** (environment `hackathon`)

   | Secret | Purpose |
   |--------|---------|
   | `AZURE_CLIENT_ID` | App registration **Application (client) ID** for `azure/login@v2` |
   | `AZURE_TENANT_ID` | Microsoft Entra **tenant ID** |
   | `AZURE_SUBSCRIPTION_ID` | Target Azure **subscription ID** |
   | `SQL_ADMIN_PASSWORD` | SQL server administrator password (never commit; not stored in `parameters.dev.json`) |

3. **Variables** (environment `hackathon`, non-secret)

   | Variable | Purpose |
   |----------|---------|
   | `SQL_ADMIN_LOGIN` | SQL admin username (passed to Bicep as `sqlAdminLogin`) |
   | `AUTH0_DOMAIN` | Auth0 tenant domain |
   | `AUTH0_AUDIENCE` | API audience |
   | `AUTH0_CLIENT_ID` | SPA client ID for demo |

4. **Optional protections**  
   - You may add required reviewers or wait timers on environment `hackathon`. Spec **PRF-001** treats approval wait as outside the “~2 hour” automation budget.

---

## 3. Demo SKU baseline and exceptions (cost posture)

Defaults in **`infra/bicep/main.bicep`** and **`infra/bicep/modules/monitoring.bicep`** aim for **lowest suitable tier** for a working demo (spec **FR-006**). Anything that is not the absolute cheapest option is listed here as an **exception with rationale**.

| Component | Tier / sizing | Rationale |
|-----------|----------------|-----------|
| Azure Container Registry | **Basic** | Lowest ACR SKU adequate for demo push/pull. |
| Azure Service Bus | **Standard** | **Exception**: template uses a **topic** with subscriptions; **Basic** does not support topics. Standard is the minimum feature-compatible tier. |
| Azure SQL Database | **S0** (Standard tier) | Low entry DTU; suitable for demo load. |
| Key Vault | **Standard** | Standard vault SKU; Premium not required for this template. |
| Container Apps environment | Consumption-style managed environment | No dedicated workload profile in template; pay for use. |
| Container Apps (each service) | **0.5 CPU / 1.0 Gi** memory (see `main.bicep`) | Small footprint per app for demo. |
| Log Analytics | **PerGB2018**, **30-day** retention | Short retention to limit retention cost; ingestion still billed per GB. |
| Application Insights | Workspace-based **web** component | Uses Log Analytics backend; aligns with `monitoring.bicep`. |

For a full machine-readable inventory of workflow inputs, secrets, variables, and smoke env vars, see **`contracts/deployment-pipeline-contract.md`**.

---

## 4. Verify before first deploy

- [ ] Federated credential **subject** matches how GitHub issues OIDC tokens for this repo and environment `hackathon`.  
- [ ] All **four** environment secrets and **four** variables in section 2 exist on environment `hackathon`.  
- [ ] Service principal has **RBAC** on the subscription or target resource group.  
- [ ] `infra/environments/hackathon/parameters.dev.json` reviewed (non-secret defaults; no `sqlAdminPassword` in file).

---

## 5. Run deployment

1. GitHub → **Actions** → workflow **deploy-hackathon** → **Run workflow**.  
2. Set inputs: `environmentName`, `location`, `resourceGroupName`, optional container image overrides, `runSmoke` (default on).  
3. Expected step order: **Validate Bicep templates and hackathon parameters JSON** → **Ensure resource group exists (idempotent)** → **ARM what-if** → **Deploy infrastructure and container apps** → **Resolve Container Apps FQDNs for smoke checks** → **Run HTTP smoke checks** (if `runSmoke`).

---

## 6. Expected duration (reference run)

Per spec **PRF-001** / **PRF-002**:

- **Full `deploy-hackathon` run** SHOULD complete within **~2 hours** on default GitHub-hosted runners; if your subscription or region is slower (quotas, large image pulls), note the actual time here after a reference run.  
- **Validate + what-if + smoke** together SHOULD add **≤ ~15 minutes** versus an apply-only run unless ARM what-if is unusually large.

**Reference measurement (fill in after a green run):**

| Measure | Value |
|---------|--------|
| Run date | _YYYY-MM-DD_ |
| Workflow run URL | _paste link_ |
| Total job duration | _e.g. 42m_ |
| Longest step(s) | _e.g. Deploy infrastructure …_ |

---

## 7. Verify outcome

- Workflow completes green.  
- Smoke step logs **Basic smoke checks passed.**  
- Open the frontend URL from the **Resolve** step logs or from Container Apps in the Azure portal.

---

## 8. SC-003 rehearsal — three consecutive successful deploys

Use this as evidence for spec **SC-003**. Replace placeholders after each green run.

| # | Date (UTC) | Workflow run URL | Notes |
|---|------------|------------------|-------|
| 1 | _YYYY-MM-DD_ | _URL_ | _optional_ |
| 2 | _YYYY-MM-DD_ | _URL_ | _optional_ |
| 3 | _YYYY-MM-DD_ | _URL_ | _optional_ |

---

## 9. Common failures and log patterns (SC-005)

Map Azure CLI / Actions output to a **category** so operators know what to fix first.

| Category | Typical log patterns / symptoms | What to fix |
|----------|----------------------------------|-------------|
| **Identity** | `AADSTS`, `No matching federated identity record`, wrong tenant, OIDC token rejected | Federated credential subject, client/tenant IDs, `azure/login` inputs |
| **Secret** | Script messages like `[secret/config] SQL_ADMIN_PASSWORD`, Key Vault / SQL login failures, “password required” | GitHub secrets, `SQL_ADMIN_LOGIN` / `SQL_ADMIN_PASSWORD`, Key Vault access |
| **RBAC** | `Authorization failed`, `403`, `does not have authorization` on subscription or RG | Role assignments for the OIDC service principal |
| **Quota** | `QuotaExceeded`, `Operation could not be completed`, capacity / SKU unavailable in region | Subscription quotas, try another region, request increase |
| **Parameter** | `[parameter]` in script output, Bicep compile errors, JSON parse errors for `parameters.dev.json` | Parameter file syntax, path to `main.bicep`, CLI `--parameters` names |
| **ARM policy** | `RequestDisallowedByPolicy`, policy definition names in error text | Azure Policy exemptions or template alignment with policy |

Legacy quick mapping table:

| Symptom | Likely category |
|---------|-----------------|
| `AADSTS` / login failure | Identity |
| `Authorization failed` / 403 on deployment | RBAC |
| Missing password / SQL errors | Secret |
| ARM quota or capacity errors | Quota |
| Wrong app names in endpoint step | Parameter / naming (`projectName`, `environmentName`) |

---

## 10. Local parity (optional)

From repo root with Azure CLI logged in and the same env vars set as in GitHub Actions:

```powershell
./scripts/infra/validate-bicep.ps1
./scripts/infra/deploy-hackathon.ps1 -WhatIf -EnvironmentName dev -Location eastus -ResourceGroupName rg-trading-hackathon-dev
```

If the resource group does not exist yet, either create it with `az group create` or pass **`-CreateResourceGroup`** to `deploy-hackathon.ps1` (the GitHub workflow always runs `az group create` before what-if/deploy).

`deploy-hackathon.ps1` requires **`SQL_ADMIN_PASSWORD`** (and typically **`SQL_ADMIN_LOGIN`**) in the environment for what-if and deploy, because Bicep requires `sqlAdminPassword` and it is not stored in `parameters.dev.json`.

Full apply uses the same script without `-WhatIf`.

---

*For machine-readable inventories, see `contracts/deployment-pipeline-contract.md`.*
