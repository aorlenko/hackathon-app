# Implementation Plan: Reliable demo deployment and cloud prerequisites

**Branch**: `009-fix-deployment-pipeline` | **Date**: 2026-03-21 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/009-fix-deployment-pipeline/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Deliver a **repeatable, documented demo deployment path** that matches repository infrastructure: fix and harden `.github/workflows/deploy-hackathon.yml`, `scripts/infra/deploy-hackathon.ps1`, `scripts/infra/validate-bicep.ps1`, and `scripts/infra/smoke-hackathon.ps1` against `infra/bicep/main.bicep` and `infra/environments/hackathon/parameters.dev.json`. Add a **single prerequisites and runbook** (see `quickstart.md` and `contracts/`) listing every GitHub secret/variable/environment and every Azure identity/federation step. Default sizing stays **demo-minimal** (see `research.md` for SKU rationale and any documented exceptions). Improve **failure messages** so operators can map errors to identity, secret, quota, or parameter issues.

## Technical Context

**Language/Version**: YAML (GitHub Actions), PowerShell (`pwsh` on `ubuntu-latest`), Bicep (ARM), Azure CLI  
**Primary Dependencies**: `actions/checkout@v4`, `azure/login@v2` (OIDC), Azure CLI (`az`, `az bicep`), GitHub Environments  
**Storage**: N/A for this feature’s code paths; provisioned stack uses Azure SQL, Key Vault, Service Bus, and Container Apps per existing Bicep  
**Testing**: `npm test` / `npm run lint` unchanged for app code; deployment feature adds workflow + script validation (`az bicep build`, JSON parameter parse), smoke script against health URLs, and documented tabletop or three-run rehearsal per spec SC-003  
**Target Platform**: GitHub Actions `ubuntu-latest` → Azure subscription (resource group scope)  
**Project Type**: infrastructure and DevOps automation (CI/CD + IaC)  
**UX Consistency Baseline**: N/A — operator-facing docs and log clarity only; no product UI changes  
**Performance Goals**: PRF-001 — full deploy run SHOULD complete within ~2 hours on default runners (document if longer); PRF-002 — validate + smoke SHOULD add ≤ ~15 minutes vs apply-only unless justified  
**Constraints**: Unattended auth via OIDC to Azure; no secrets in source; cheapest suitable SKUs per FR-006; manual `workflow_dispatch` acceptable (out of scope: deploy every merge)  
**Scale/Scope**: Single demo/hackathon environment profile; small concurrency; cost predictability over throughput

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Code Quality Gate**: Workflow YAML, PowerShell deploy/validate/smoke scripts, and Bicep remain subject to repo CI (`ci.yml` / lint conventions). Changes keep scripts modular (`validate-bicep.ps1` vs `deploy-hackathon.ps1`) and remove dead parameters where found.
- **Testing Gate**: Automated checks: Bicep build + parameter JSON validation in deploy workflow; smoke script for HTTP health endpoints. No new unit tests for “business logic” (none in scope); cross-boundary behavior is **GitHub ↔ Azure ARM** and **deployed URLs ↔ health checks** — covered by workflow + smoke + documented rehearsal runs.
- **UX Consistency Gate**: **Not applicable** — no end-user product UI. Prerequisites/runbook must follow clear, ordered steps and consistent terminology (GitHub vs Azure).
- **Performance Gate**: Spec PRF-001/PRF-002 adopted as budgets; runbook will note expected duration and longest steps once measured on a reference run.
- **Simplicity Gate**: Prefer extending `deploy-hackathon.yml` and existing scripts over new pipeline products; document OIDC + one GitHub Environment (`hackathon`) rather than introducing parallel systems.

### Post–Phase 1 re-evaluation

Design artifacts (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) align with constitution: no speculative abstractions; verification is measurable (smoke + rehearsal). No new complexity tracking required.

## Project Structure

### Documentation (this feature)

```text
specs/009-fix-deployment-pipeline/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
.github/workflows/
├── deploy-hackathon.yml   # Manual demo deploy workflow
└── ci.yml                 # CI (incl. optional Azure image job when secrets present)

infra/
├── bicep/
│   ├── main.bicep         # Demo stack (Container Apps, SQL, Service Bus, ACR, KV, …)
│   └── modules/
│       └── monitoring.bicep
└── environments/hackathon/
    └── parameters.dev.json

scripts/infra/
├── deploy-hackathon.ps1   # az deployment group create / what-if
├── validate-bicep.ps1     # az bicep build + parameter JSON check
└── smoke-hackathon.ps1    # HTTP smoke to frontend + /health endpoints
```

**Structure Decision**: This feature touches **GitHub Actions workflows**, **PowerShell infra scripts**, and **Bicep** under `infra/` — no new application packages. Documentation lives under `specs/009-fix-deployment-pipeline/` with contracts for GitHub/Azure expectations.

## Complexity Tracking

> No constitution violations requiring justification for this feature.
