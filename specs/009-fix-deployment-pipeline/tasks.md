# Tasks: Reliable demo deployment and cloud prerequisites

**Input**: Design documents from `C:\work\my\specit_trading_test\specs\009-fix-deployment-pipeline\`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: No product unit-test suite is in scope. User stories are verified with **operational checks** (local `az`/PowerShell validation, workflow runs, secret/contract cross-checks, SKU audit) per spec.md independent test criteria and SC-003 rehearsal — not TDD-style code tests.

**Organization**: Phases follow user story priority (P1 → P2 → P3) after shared setup and foundational reconciliation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks in the same phase)
- **[Story]**: User story label ([US1], [US2], [US3]) only in user-story phases
- Every task description includes at least one concrete file path

## Path Conventions (this repo)

- Workflows: `.github/workflows/`
- Infra scripts: `scripts/infra/`
- Bicep: `infra/bicep/`, `infra/environments/hackathon/`
- Feature docs: `specs/009-fix-deployment-pipeline/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Baseline the current automation against the specification before changing behavior.

- [x] T001 Audit `.github/workflows/deploy-hackathon.yml`, `scripts/infra/deploy-hackathon.ps1`, `scripts/infra/validate-bicep.ps1`, `scripts/infra/smoke-hackathon.ps1`, `infra/bicep/main.bicep`, and `infra/environments/hackathon/parameters.dev.json` against `specs/009-fix-deployment-pipeline/spec.md` FR-001, FR-004, FR-005, and FR-007; record gaps in working notes (optional: `specs/009-fix-deployment-pipeline/research.md` addendum)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Lock the **machine-readable contract** and **naming alignment** so user-story work does not drift (SC-002, data-model validation rules).

**⚠️ CRITICAL**: Complete this phase before implementation tasks in US1–US3 that change workflow inputs or secrets.

- [x] T002 Reconcile `specs/009-fix-deployment-pipeline/contracts/deployment-pipeline-contract.md` sections A–F with the live `.github/workflows/deploy-hackathon.yml` (dispatch inputs, `permissions`, `environment: hackathon`, `secrets.*`, `vars.*`, smoke env wiring); update the contract file for any drift
- [x] T003 [P] Verify `scripts/infra/deploy-hackathon.ps1` CLI `--parameters` overrides match `infra/bicep/main.bicep` parameter names (`sqlAdminLogin`, `sqlAdminPassword`, `auth0Domain`, `auth0Audience`, `auth0ClientId`, `alertEmailAddress`, image parameters); fix script or Bicep if mismatched
- [x] T004 [P] Verify the **Resolve container app endpoints** step in `.github/workflows/deploy-hackathon.yml` uses `infra/environments/hackathon/parameters.dev.json` `parameters.projectName.value` and app name suffix `${{ inputs.environmentName }}` per `specs/009-fix-deployment-pipeline/contracts/deployment-pipeline-contract.md`; fix workflow or contract if naming diverges from `infra/bicep/main.bicep`

**Checkpoint**: Contract, workflow, Bicep parameters, and Container App naming are internally consistent.

---

## Phase 3: User Story 1 — Repeatable release to the demo environment (Priority: P1) 🎯 MVP

**Goal**: Operators can run the standard GitHub Actions deploy path and reach successful validation, deploy, endpoint resolution, and optional smoke with **actionable, classifiable failures** (spec US1, FR-004, FR-005, SC-005).

**Independent Test**: From a documented starting point, an operator follows `specs/009-fix-deployment-pipeline/quickstart.md` only and achieves a green `deploy-hackathon` run with demo endpoints reachable when smoke is enabled.

### Verification for User Story 1

- [x] T005 [P] [US1] Run `scripts/infra/validate-bicep.ps1` from repository root; resolve failures in `infra/bicep/main.bicep`, `infra/bicep/modules/monitoring.bicep`, or `infra/environments/hackathon/parameters.dev.json`
- [x] T006 [US1] Run `scripts/infra/deploy-hackathon.ps1` with `-WhatIf` from repository root using env vars described in `specs/009-fix-deployment-pipeline/quickstart.md`; fix `scripts/infra/deploy-hackathon.ps1` or templates if the command fails unexpectedly

### Implementation for User Story 1

- [x] T007 [P] [US1] Add preflight checks and operator-oriented error classification hints in `scripts/infra/deploy-hackathon.ps1` (e.g. missing `SQL_ADMIN_PASSWORD` / `SQL_ADMIN_LOGIN`, `az` / ARM failures) per `specs/009-fix-deployment-pipeline/research.md` R-006
- [x] T008 [P] [US1] Improve failure output in `scripts/infra/validate-bicep.ps1` when `az bicep build` or `infra/environments/hackathon/parameters.dev.json` JSON parse fails (include path and next-step hint)
- [x] T009 [P] [US1] Improve failure output in `scripts/infra/smoke-hackathon.ps1` for missing `FRONTEND_URL` / `*_API_URL` env vars and non-2xx responses; align behavior with `specs/009-fix-deployment-pipeline/contracts/deployment-pipeline-contract.md` section F
- [x] T010 [US1] Harden `.github/workflows/deploy-hackathon.yml` (clear step names, idempotent `az group create` behavior called out, `pwsh` steps consistent on `ubuntu-latest`, outputs passed correctly to smoke) per `specs/009-fix-deployment-pipeline/plan.md`
- [x] T011 [US1] After a reference run, document expected duration and longest steps in `specs/009-fix-deployment-pipeline/quickstart.md` per `specs/009-fix-deployment-pipeline/spec.md` PRF-001 and PRF-002

**Checkpoint**: Local validate + what-if succeed; workflow path is coherent; operators get categorized failure hints.

---

## Phase 4: User Story 2 — Clear prerequisite setup for new environments (Priority: P2)

**Goal**: One prerequisites/runbook path lists **every** GitHub secret, variable, environment, and Azure OIDC/RBAC step (spec US2, FR-002, FR-003, SC-002).

**Independent Test**: A reader with repo + Azure admin access completes setup using only published docs and can authenticate and run deploy without undocumented keys.

### Verification for User Story 2

- [x] T012 [P] [US2] Grep `.github/workflows/deploy-hackathon.yml` for `secrets.` and `vars.` references; confirm each name is documented in `specs/009-fix-deployment-pipeline/contracts/deployment-pipeline-contract.md` and `specs/009-fix-deployment-pipeline/quickstart.md` (SC-002 cross-check)

### Implementation for User Story 2

- [x] T013 [P] [US2] Expand `specs/009-fix-deployment-pipeline/quickstart.md` with ordered Azure (subscription, RG, app registration, federated credential subject for `environment:hackathon`, RBAC) then GitHub (`hackathon` environment, secrets, variables, optional protections) per `specs/009-fix-deployment-pipeline/spec.md` FR-003
- [x] T014 [P] [US2] Update `specs/009-fix-deployment-pipeline/contracts/deployment-pipeline-contract.md` for any workflow/script env changes introduced in `.github/workflows/deploy-hackathon.yml` or `scripts/infra/deploy-hackathon.ps1` during US1
- [x] T015 [US2] Align the prerequisites inventory in `specs/009-fix-deployment-pipeline/data-model.md` section 2 with the final secret/variable tables in `specs/009-fix-deployment-pipeline/contracts/deployment-pipeline-contract.md`

**Checkpoint**: Prerequisites doc + contract + data-model inventory match the automation surface area.

---

## Phase 5: User Story 3 — Demo-appropriate cost posture (Priority: P3)

**Goal**: Defaults are **lowest suitable tier** with explicit documented exceptions (spec US3, FR-006, SC-004).

**Independent Test**: A reviewer reads `infra/bicep/main.bicep`, `infra/bicep/modules/monitoring.bicep`, `infra/environments/hackathon/parameters.dev.json`, and the exceptions list in docs and confirms demo-minimal posture within 30 minutes.

### Verification for User Story 3

- [x] T016 [P] [US3] Audit SKUs/sizing in `infra/bicep/main.bicep` (and nested module usage) and `infra/bicep/modules/monitoring.bicep` against `specs/009-fix-deployment-pipeline/research.md` R-004; list any drift

### Implementation for User Story 3

- [x] T017 [P] [US3] Document demo SKU baseline and **exceptions** (component + tier + rationale, e.g. Service Bus Standard) in `specs/009-fix-deployment-pipeline/quickstart.md`, referencing `infra/bicep/main.bicep` / `infra/bicep/modules/monitoring.bicep`
- [x] T018 [US3] Update `specs/009-fix-deployment-pipeline/research.md` R-004 table if live Bicep differs from the documented baseline; keep `specs/009-fix-deployment-pipeline/contracts/deployment-pipeline-contract.md` change-control note G in mind if new parameters affect cost-related flags

**Checkpoint**: SKU story is auditable from code + docs with no silent “production default.”

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Rehearsal evidence, failure taxonomy, CI compatibility.

- [x] T019 [P] Add an SC-003 **three successful consecutive deploy** rehearsal checklist (dates/run URLs placeholders) to `specs/009-fix-deployment-pipeline/quickstart.md` per `specs/009-fix-deployment-pipeline/spec.md` SC-003
- [x] T020 Extend **Common failures** in `specs/009-fix-deployment-pipeline/quickstart.md` with explicit categories (identity, secret, quota, parameter, ARM policy) and mapping to log patterns per `specs/009-fix-deployment-pipeline/spec.md` SC-005
- [x] T021 Confirm `.github/workflows/ci.yml` still passes / remains consistent with repo conventions after workflow and `scripts/infra/*.ps1` edits (YAML validity, no accidental breakage of related jobs)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 — **blocks** US1–US3 contract-sensitive edits
- **Phase 3 (US1)**: Depends on Phase 2 — delivers working automation path
- **Phase 4 (US2)**: Depends on US1 **if** workflow/env changed in US1; otherwise can start after Phase 2 for doc-only gaps — **recommended after US1** so docs match final YAML
- **Phase 5 (US3)**: Can start after Phase 2 for pure SKU audit; doc updates should follow any Bicep changes from US1
- **Phase 6 (Polish)**: After US1 at minimum; full polish after US2–US3

### User Story Dependencies

```text
US1 (P1) ──► US2 (P2)   (docs should reflect final workflow)
     │
     └────────► US3 (P3) (can overlap US2 when files differ)
```

### Within Each User Story

- Run **Verification** tasks before or alongside **Implementation** tasks where noted
- Keep `specs/009-fix-deployment-pipeline/contracts/deployment-pipeline-contract.md` synchronized with `.github/workflows/deploy-hackathon.yml` on any `secrets.*` / `vars.*` change (contract section G)

### Parallel Opportunities

- **Phase 2**: T003 and T004 in parallel
- **US1**: T005 parallel with early prep; T007–T009 in parallel (different script files); T010 workflow after or in parallel if no script coupling
- **US2**: T012–T014 in parallel; T015 after contract/quickstart text stabilizes
- **US3**: T016 and T017 in parallel; T018 follows audit conclusions
- **Polish**: T019 and T020 in parallel

---

## Parallel Example: User Story 1

```text
# Parallel script hardening (different files):
T007  scripts/infra/deploy-hackathon.ps1
T008  scripts/infra/validate-bicep.ps1
T009  scripts/infra/smoke-hackathon.ps1

# Parallel local verification before merge:
T005  scripts/infra/validate-bicep.ps1 (execute)
T006  scripts/infra/deploy-hackathon.ps1 -WhatIf (execute)
```

---

## Parallel Example: User Story 2

```text
T012  .github/workflows/deploy-hackathon.yml → grep vs contract
T013  specs/009-fix-deployment-pipeline/quickstart.md
T014  specs/009-fix-deployment-pipeline/contracts/deployment-pipeline-contract.md
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1–2 (audit + contract/script/template alignment)
2. Complete Phase 3 (US1): validate, what-if, workflow hardening, operator errors, quickstart timing note
3. **STOP and VALIDATE**: Green `deploy-hackathon` run + smoke optional per inputs

### Incremental Delivery

1. US1 → demo path reliable
2. US2 → onboarding and SC-002 completeness
3. US3 → cost posture transparency
4. Polish → SC-003/SC-005 rehearsal and taxonomy

### Parallel Team Strategy

- Developer A: US1 workflow + deploy script (`deploy-hackathon.yml`, `deploy-hackathon.ps1`)
- Developer B: US1 validate + smoke scripts (`validate-bicep.ps1`, `smoke-hackathon.ps1`)
- After US1: Developer C: US2 docs (`quickstart.md`, `contracts/`, `data-model.md`) while Developer D: US3 Bicep SKU audit

---

## Notes

- `[P]` = different files, no hard ordering within the same bullet batch
- Re-run the SC-002 grep whenever `.github/workflows/deploy-hackathon.yml` gains new `secrets.*` or `vars.*`
- Commit after each task or logical group; stop at checkpoints to validate the story independently

---

## Format validation

- All tasks use the checklist pattern: `- [ ] Tnnn ...` with sequential IDs **T001–T021**
- **[Story]** labels appear only on **US1–US3** phase tasks
- **[P]** appears only where tasks are parallel-safe per file boundaries
- Every task description names at least one concrete path under the repo or `specs/009-fix-deployment-pipeline/`
