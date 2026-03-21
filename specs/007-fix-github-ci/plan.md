# Implementation Plan: Reliable continuous integration

**Branch**: `007-fix-github-ci` | **Date**: 2026-03-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-fix-github-ci/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Restore **trust in GitHub Actions** for this repo: the **CI workflow** (`.github/workflows/ci.yml`) must go **green** for healthy changes and stay **diagnosable** when something legitimately fails. Work is **limited to CI**—**do not** edit deployment/release workflows (e.g. `deploy-hackathon.yml`) unless the spec’s documented minimal-exception rule applies. The likely work areas are **runner tool parity** (Node 20, .NET 8, PowerShell, Docker Compose, Azure CLI / Bicep as required by `scripts/infra/validate-bicep.ps1`), **job conditions and `needs` graphs**, and **failing tests or lint** surfaced by the frontend/backend jobs. Detailed decisions and investigation order are in `research.md`; CI structure is modeled in `data-model.md` and `contracts/ci-workflow.md`. Local parity steps are in `quickstart.md`.

## Technical Context

**Language/Version**: YAML (GitHub Actions), PowerShell 7 (`pwsh`), Node.js 20, .NET 8 SDK  
**Primary Dependencies**: `actions/checkout`, `actions/setup-node`, `actions/setup-dotnet`, `azure/login` (only in non-PR image job—out of scope to redesign); repo scripts `npm run compose:config`, `npm run infra:validate`  
**Storage**: N/A (no data store changes)  
**Testing**: Validation via green CI runs; optional local commands mirroring jobs (`dotnet test`, `npm test` / `npm run lint` in `apps/frontend-spa`)  
**Target Platform**: `ubuntu-latest` GitHub-hosted runners  
**Project Type**: Monorepo CI (infra compose + Bicep compile, SPA, .NET solution)  
**UX Consistency Baseline**: N/A for product UI; check names and log clarity in Actions should remain understandable (aligns with FR-002)  
**Performance Goals**: Per spec PRF-001/PRF-002—preserve or document workflow duration; use GitHub Actions run duration as the measurable budget  
**Constraints**: FR-003—no deployment workflow changes; fix only what gates merging and integration-branch health  
**Scale/Scope**: One primary workflow file (`ci.yml`), supporting scripts only if required for CI honesty (e.g. `validate-bicep.ps1` or package scripts)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-design (initial)

- **Code Quality Gate**: **Pass** — CI continues to run existing lint/static/build steps; any workflow edit keeps clear job/step names (FR-002).
- **Testing Gate**: **Pass** — correctness still enforced by existing `dotnet test` and frontend tests in CI; this feature may fix false failures (flaky or misconfigured) but does not replace tests.
- **UX Consistency Gate**: **Pass (N/A)** — no end-user product UI; Actions UX (clear failing step) is explicitly in scope via FR-002.
- **Performance Gate**: **Pass** — use Actions **run duration** and optional **job timing** as evidence; document any intentional slowdown (PRF-002).
- **Simplicity Gate**: **Pass** — prefer the smallest workflow or prerequisite fix (e.g. add a missing tool step) over new abstractions or extra workflows.

### Post-design (Phase 1)

- **All gates**: **Pass** — `data-model.md` and `contracts/ci-workflow.md` bound the automation surface; `quickstart.md` ties local commands to CI. No constitution violations requiring the complexity table.

## Project Structure

### Documentation (this feature)

```text
specs/007-fix-github-ci/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── ci-workflow.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
.github/workflows/
└── ci.yml                 # Primary CI workflow (in scope)

package.json               # compose:config, infra:validate scripts

scripts/infra/
└── validate-bicep.ps1     # Bicep build + parameter JSON parse (invoked from CI)

infra/bicep/               # Templates validated by validate-bicep.ps1
infra/environments/hackathon/
└── parameters.dev.json

apps/frontend-spa/         # Frontend job (lint, test, build)

apps/services/
└── TradingPlatform.sln    # Backend job (restore, build, test)
```

**Structure Decision**: Changes concentrate on `.github/workflows/ci.yml` and, only if necessary, root `package.json` or `scripts/infra/validate-bicep.ps1` to match runner capabilities—**not** on `deploy-hackathon.yml` or hackathon deploy scripts per FR-003.

## Complexity Tracking

> No constitution violations required justification for this plan.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## CI workflow duration (PRF-002)

**Before change**: Record the median wall-clock duration of successful `ci` workflow runs from the GitHub Actions run list (filter: workflow `ci`, conclusion success) before merging this work.

**After change**: Recompute the median after the updated workflow is on the default branch. This delivery adds an apt-based install of Azure CLI and PowerShell on `infra-validate`, which **increases** that job’s setup time by roughly one to three minutes; the tradeoff is an **honest** infra gate that matches `scripts/infra/validate-bicep.ps1` and local `quickstart.md` expectations (runner parity).
