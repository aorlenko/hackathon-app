# Research: 009-fix-deployment-pipeline

Consolidated decisions for demo deployment automation and prerequisites. No open **NEEDS CLARIFICATION** items — all items grounded in current repo behavior and Azure/GitHub patterns.

---

## R-001: Azure authentication from GitHub Actions

- **Decision**: Use **OpenID Connect (workload identity federation)** via `azure/login@v2` with `client-id`, `tenant-id`, and `subscription-id` from GitHub secrets, plus `permissions: id-token: write` (already in `deploy-hackathon.yml`).
- **Rationale**: Matches FR-002 (unattended, no long-lived client secrets in the workflow when federation is configured). Aligns with existing `ci.yml` pattern.
- **Alternatives considered**: (1) Client secret in `AZURE_CREDENTIALS` — simpler setup but violates “prefer federation” and secret rotation burden; (2) Managed identity on self-hosted runner — higher ops cost for a demo.

---

## R-002: GitHub Environment `hackathon`

- **Decision**: Keep deployment gated to GitHub Environment **`hackathon`** (`environment: hackathon` in workflow) for secrets/variables and optional protection rules.
- **Rationale**: Centralizes demo credentials and non-secret config (`vars.*`) separate from repository defaults; supports approval gates if the team enables them later.
- **Alternatives considered**: Repository-level secrets only — works but weakens audit and approval boundaries for production-like credentials.

---

## R-003: Bicep validation before apply

- **Decision**: Continue **`az bicep build`** on `main.bicep` and `modules/monitoring.bicep`, plus **JSON parse** of `parameters.dev.json` in `validate-bicep.ps1`, invoked before what-if/deploy in the workflow (FR-004).
- **Rationale**: Fast local and CI feedback; does not require live Azure calls. `az deployment group what-if` provides second-stage validation against ARM.
- **Alternatives considered**: `az deployment group validate` only — redundant with what-if for many cases; skipping build — misses syntax errors early.

---

## R-004: Demo SKU / tier posture (FR-006)

- **Decision**: Treat current `main.bicep` defaults as the **demo baseline** and document them in the prerequisites/runbook with an **exceptions** subsection where “minimum” is not the absolute cheapest Azure offers:

| Component | Current choice | Notes |
|-----------|----------------|--------|
| ACR | Basic | Lowest ACR SKU; suitable for demo throughput. |
| Azure Service Bus | Standard | **Exception**: Topics (and partitioning) require Standard+; Basic does not support topics — documented as required for architecture, not “overspend.” |
| Azure SQL Database | S0 (Standard tier) | Low entry DTU tier; document if Basic/general-purpose alternative is ever evaluated (compatibility/features). |
| Key Vault | Standard SKU | Typical for secrets; Premium not used. |
| Container Apps | Consumption-style environment (no dedicated workload profile in template) | Pay-for-use; small CPU/memory per container in template. |
| Log Analytics workspace | **PerGB2018**, **30-day** retention (`monitoring.bicep`) | Pay-as-you-go ingestion; retention kept short for demo cost. |
| Application Insights | Workspace-based component (`kind: web`, ingestion via Log Analytics) | Tied to same workspace; no Premium SKU on Insights. |

- **Rationale**: “Cheapest” means **lowest tier that supports required features** (spec assumption).
- **Alternatives considered**: Downgrade Service Bus to Basic — **rejected** (topics/subscriptions are required by current Bicep).

**SKU audit (T016, 2026-03-21):** Reconciled live `infra/bicep/main.bicep` and `infra/bicep/modules/monitoring.bicep` with this table. **No silent drift**: Service Bus remains Standard for topics; SQL S0; ACR Basic; Key Vault **standard**; Container Apps **0.5 CPU / 1.0 Gi** per service template; Log Analytics **PerGB2018** with **30-day** retention; Application Insights workspace-based **web**. Prior wording “confirm SKUs in module” is resolved by the explicit Log Analytics / App Insights rows above.

---

## R-005: Post-deploy verification

- **Decision**: Keep **`smoke-hackathon.ps1`** as the minimal automated check (frontend GET + `/health` on three APIs). Workflow resolves FQDNs via `az containerapp show` after deploy (FR-005).
- **Rationale**: Matches existing endpoints; fast. Warning in script about full lifecycle E2E remains honest scope boundary.
- **Alternatives considered**: Full Playwright E2E in deploy job — heavier, brittle for infra-focused pipeline; defer to optional follow-up.

---

## R-006: Operator error clarity

- **Decision**: During implementation, add **preflight checks** in PowerShell (e.g., missing env vars with explicit names) and normalize **Azure CLI / ARM error excerpts** in workflow logs where feasible; runbook maps common failures (auth, RBAC, quota, wrong subscription) to fix categories (FR-002, SC-005).
- **Rationale**: Spec emphasizes classifiable failures; full ARM message control is limited without wrapping every `az` call.
- **Alternatives considered**: Silent retry loops — hides root cause; rejected.

---

## R-007: Resource group reuse vs clean slate

- **Decision**: Document **idempotent `az deployment group`** behavior: same resource group can be updated; name collisions handled by `uniqueString` patterns in Bicep for global names. Operators choosing a **new** RG get a fresh stack; **reuse** updates in place. Partial failure handling follows ARM transaction semantics per resource; runbook notes retry and `what-if` usage.
- **Rationale**: Matches how `deploy-hackathon.ps1` is invoked today.
- **Alternatives considered**: Mandatory RG delete before deploy — safer “clean slate” but destructive and not required by spec.

---

## Implementation audit addendum (T001, 2026-03-21)

Baseline review of `.github/workflows/deploy-hackathon.yml`, `scripts/infra/deploy-hackathon.ps1`, `validate-bicep.ps1`, `smoke-hackathon.ps1`, `infra/bicep/main.bicep`, and `infra/environments/hackathon/parameters.dev.json` against **FR-001** (repeatable path), **FR-004** (validate before apply), **FR-005** (post-deploy checks), and **FR-007** (alignment with Bicep/parameters):

| Area | Finding |
|------|---------|
| FR-004 | Workflow runs `validate-bicep.ps1` before what-if/deploy; script builds `main.bicep`, `modules/monitoring.bicep`, and parses `parameters.dev.json`. |
| FR-005 | Workflow resolves FQDNs then runs `smoke-hackathon.ps1` when `runSmoke` is true. |
| FR-001 / FR-007 | Deploy script passes `@parameters.dev.json` plus `environmentName` and optional CLI overrides whose names match Bicep `param` identifiers; Container App names in the resolve step use `projectName` from the parameters file and workflow `environmentName`. |
| Gaps addressed in implementation | Clearer operator error hints in scripts, idempotent `az group create` called out in workflow, expanded runbook and failure taxonomy in `quickstart.md`. |
