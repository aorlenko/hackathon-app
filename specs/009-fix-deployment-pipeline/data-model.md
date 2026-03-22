# Data model: 009-fix-deployment-pipeline

This feature is **process and configuration oriented**. Entities below are **logical** (spec “Key Entities”), not relational tables. They drive documentation, contracts, and cross-checks against workflows and Bicep.

---

## 1. Deployment automation definition

| Field | Description |
|-------|-------------|
| `workflowName` | e.g. `deploy-hackathon` |
| `trigger` | `workflow_dispatch` |
| `inputs` | `environmentName`, `location`, `resourceGroupName`, `runSmoke` |
| `jobEnvironment` | GitHub Environment name (e.g. `hackathon`) |
| `steps` | Checkout → Azure login → validate Bicep → ensure RG → what-if → deploy → resolve endpoints → optional smoke |

**Relationships**: Consumes **Prerequisites inventory**; targets **Demo infrastructure profile** in Azure.

**Validation rules**: Inputs must match what `deploy-hackathon.ps1` and Bicep parameters expect; secret names must match contract.

---

## 2. Prerequisites inventory

Aligned with **`contracts/deployment-pipeline-contract.md`** sections C and D (SC-002).

**GitHub Environment `hackathon` — secrets**

| Name |
|------|
| `AZURE_CLIENT_ID` |
| `AZURE_TENANT_ID` |
| `AZURE_SUBSCRIPTION_ID` |
| `SQL_ADMIN_PASSWORD` |

**GitHub Environment `hackathon` — variables**

| Name |
|------|
| `SQL_ADMIN_LOGIN` |
| `AUTH0_DOMAIN` |
| `AUTH0_AUDIENCE` |
| `AUTH0_CLIENT_ID` |

**Azure (outside GitHub)**

| Category | Requirement |
|----------|-------------|
| **Tenant setup** | App registration + **federated credential** for GitHub OIDC (issuer `https://token.actions.githubusercontent.com`; subject e.g. `repo:ORG/REPO:environment:hackathon`) |
| **RBAC** | Deployment identity can deploy to target subscription or resource group (e.g. Contributor on RG, per org policy) |

**Relationships**: Required by **Deployment automation definition**; must list 100% of keys referenced in workflow (SC-002).

**Validation rules**: Automated cross-check: grep workflow + scripts for `secrets.` and `vars.` and reconcile with doc table.

---

## 3. Demo infrastructure profile

| Field | Description |
|-------|-------------|
| `template` | `infra/bicep/main.bicep` |
| `defaultParametersFile` | `infra/environments/hackathon/parameters.dev.json` |
| `skuPolicy` | Lowest suitable tier per component; documented exceptions (e.g. Service Bus Standard for topics) |
| `region` | Workflow input `location` (default e.g. `eastus`) |
| `resourceGroup` | Workflow input `resourceGroupName` |

**Relationships**: Realized as Azure resources; images and Auth0 placeholders overridden via parameters and env.

**Validation rules**: Any non-minimum SKU must have **component + tier + rationale** (FR-006).

---

## 4. Verification record

| Field | Description |
|-------|-------------|
| `workflowRunUrl` | Link to successful GitHub Actions run |
| `smokeLog` | Output of `smoke-hackathon.ps1` when `runSmoke` true |
| `endpoints` | Resolved `frontend_url`, `market_api_url`, `trade_api_url`, `settlement_api_url` job outputs |

**Relationships**: Produces evidence for SC-001, SC-003, SC-005 rehearsals.

**Validation rules**: Smoke requires HTTP 2xx on frontend and `*/health` paths currently used by script.

---

## State transitions

- **Prerequisites**: incomplete → complete (manual, guided by runbook).
- **Deployment run**: `queued` → `validating` (Bicep build) → `what-if` → `deploying` → `verifying` (smoke) → `succeeded` | `failed`.
- **Failure**: Should remain **classified** (identity, secret, quota, parameter, ARM policy) per runbook, not an unlabeled terminal state.
