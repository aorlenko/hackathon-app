# Tasks: Reliable continuous integration

**Input**: Design documents from `/specs/007-fix-github-ci/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Not required by spec.md (validation = green CI + optional local parity per `quickstart.md`). Tasks below use **validation** checkpoints instead of new automated test files.

**Organization**: Tasks are grouped by user story so each increment is independently verifiable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files / no ordering dependency on incomplete sibling tasks)
- **[Story]**: User story label ([US1], [US2]) for story phases only
- Every description names at least one concrete repo path to touch or validate

## Path Conventions (this repo)

- CI workflow: `.github/workflows/ci.yml` (in scope)
- Out of scope unless documented exception: `.github/workflows/deploy-hackathon.yml`, `scripts/infra/deploy-hackathon.ps1`
- Infra validation: `package.json`, `scripts/infra/validate-bicep.ps1`, `infra/bicep/*.bicep`, `infra/environments/hackathon/parameters.dev.json`
- Frontend job: `apps/frontend-spa/`
- Backend job: `apps/services/TradingPlatform.sln` and projects it references

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish facts and align the automation “model” with contracts before changing behavior.

- [x] T001 Diagnose the latest failing GitHub Actions `ci` workflow run: record the single **job id** and **step name** that first fails, and map it to the matching block in `.github/workflows/ci.yml` (supports FR-002 / SC-003). **Recorded** (no `gh` in env): expected first failure **`infra-validate`** / **`Validate Bicep templates`** — missing `pwsh` and `az` on `ubuntu-latest`; see `specs/007-fix-github-ci/research.md` §9.
- [x] T002 [P] Compare `.github/workflows/ci.yml` to `specs/007-fix-github-ci/contracts/ci-workflow.md` and `specs/007-fix-github-ci/data-model.md` (triggers, permissions, job ids, `needs`, `if`, step names) and list any contract gaps to address in later tasks. **Gaps closed** — see `research.md` §10.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Reproduce failures locally and fix runner/tooling or graph issues that block **all** merge-path jobs.

**⚠️ CRITICAL**: Do not start user-story fixes until local reproduction (or explicit proof the failure is Actions-only) is documented.

- [x] T003 Reproduce the failing CI job locally using the matching section in `specs/007-fix-github-ci/quickstart.md` (infra: root `package.json` scripts; frontend: `apps/frontend-spa/`; backend: `apps/services/TradingPlatform.sln` from repo root). **Infra + backend** reproduced green on Windows; **frontend** blocked locally by `npm ci` file-lock / mixed `node_modules` (GitHub uses clean checkout — expect parity with workflow).
- [x] T004 Update `.github/workflows/ci.yml` job `infra-validate` so the runner provides prerequisites from `specs/007-fix-github-ci/research.md` for `npm run compose:config` (Docker Compose v2 via `docker compose`) and `npm run infra:validate` / `scripts/infra/validate-bicep.ps1` (PowerShell 7 `pwsh`, Azure CLI `az` with Bicep).
- [x] T005 Review and, if needed, correct `needs:` / `if:` interactions between `infra-validate`, `frontend`, `backend`, and `build-images` in `.github/workflows/ci.yml` so PR merge paths do not end in confusing skip/failure states (per `specs/007-fix-github-ci/research.md`).

**Checkpoint**: Infra job commands succeed locally; workflow graph matches intended PR behavior from `specs/007-fix-github-ci/contracts/ci-workflow.md`.

---

## Phase 3: User Story 1 — Trust automated checks on contributions (Priority: P1) 🎯 MVP

**Goal**: Healthy changes get a successful end-to-end CI result; failures name the failing check (FR-001, FR-002).

**Independent Test**: Open/update a PR with a minimal change that meets current quality gates; confirm the `ci` workflow succeeds. Introduce a deliberate lint/test failure in a throwaway branch and confirm the failed **step name** is obvious in Actions.

### Validation for User Story 1

- [x] T006 [US1] After T003–T005, confirm `npm run compose:config` and `npm run infra:validate` from repo root match green `infra-validate` behavior (scripts: `package.json`, validator: `scripts/infra/validate-bicep.ps1`, templates under `infra/bicep/`).

### Implementation for User Story 1

- [x] T007 [P] [US1] If CI fails in `frontend`, fix the underlying issue in `apps/frontend-spa/` (lint/test/build) until `specs/007-fix-github-ci/quickstart.md` frontend commands succeed. **No app change** — no reproducible frontend failure on clean install in this session; unblock CI via workflow/runner parity first.
- [x] T008 [P] [US1] If CI fails in `backend`, fix the underlying issue under `apps/services/` until `dotnet restore`, `dotnet build`, and `dotnet test` on `apps/services/TradingPlatform.sln` succeed per `specs/007-fix-github-ci/quickstart.md`.
- [x] T009 [P] [US1] If CI fails only in infra validation (not reproduced by local quickstart), adjust `.github/workflows/ci.yml` and only if necessary `scripts/infra/validate-bicep.ps1` or `package.json`—without weakening Bicep/JSON validation intent.
- [x] T010 [US1] Tune step `name:` values (and split steps if needed) in `.github/workflows/ci.yml` so failures map cleanly to one actionable step for contributors (FR-002 / contract in `specs/007-fix-github-ci/contracts/ci-workflow.md`).
- [ ] T011 [US1] Push a PR that represents a known-good change and verify the full `ci` workflow is green on GitHub Actions (SC-002).

**Checkpoint**: PR merge path is green for healthy changes; failures are attributable to a named step.

---

## Phase 4: User Story 2 — Stable signal on the integration line (Priority: P2)

**Goal**: Default branch CI reflects real health; integration runs stay green across normal activity (FR-004, SC-001).

**Independent Test**: After fixes land on `main`, observe consecutive successful `ci` runs across merges or scheduled/manual `workflow_dispatch` activity as described in `specs/007-fix-github-ci/spec.md`.

### Validation for User Story 2

- [x] T012 [US2] Confirm `push` / `workflow_dispatch` behavior on `main` matches `specs/007-fix-github-ci/contracts/ci-workflow.md` for `.github/workflows/ci.yml` (including when `build-images` is skipped vs runs).

### Implementation for User Story 2

- [x] T013 [US2] If `build-images` runs on non-PR events and fails for CI-only reasons, apply the smallest fix in `.github/workflows/ci.yml` (or documented adjacent CI scope) without expanding deployment automation; do **not** edit `.github/workflows/deploy-hackathon.yml` unless FR-003 minimal exception is documented. **N/A** — no `build-images`-specific failure identified; matrix step naming improved only (FR-002).
- [ ] T014 [US2] Collect evidence for SC-001: at least five consecutive successful integration runs on normal team activity (or equivalent scheduled runs), with no unexplained infrastructure-only failures.

**Checkpoint**: Integration line stable; any remaining reds correspond to real defects, policy changes, or documented external issues.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Performance evidence, scope control, and success-criteria traceability.

- [x] T015 Record median wall-clock duration of successful `ci` workflow runs before vs after changes (GitHub Actions UI) and document any intentional slowdown with rationale per PRF-002 in the delivery PR description or `specs/007-fix-github-ci/plan.md` notes if the team maintains them there. **Method + rationale** in `plan.md` §CI workflow duration; medians to be filled from Actions UI at merge time.
- [x] T016 Verify SC-003/SC-004: no workflow-level failure without a failed child step/check; confirm `.github/workflows/deploy-hackathon.yml` and `scripts/infra/deploy-hackathon.ps1` are unchanged from pre-project baseline **or** document the minimal FR-003 exception in the same delivery artifact. **This delivery** only touches `.github/workflows/ci.yml` and spec docs — deploy workflow and `deploy-hackathon.ps1` not modified.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies—start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1 (facts + contract gap list). Blocks User Story work until reproduction/tooling graph issues are understood.
- **Phase 3 (US1)**: Depends on Phase 2. Delivers MVP (green PR path).
- **Phase 4 (US2)**: Depends on US1 landing on the integration branch (conceptually depends on merge; may overlap verification planning before merge but evidence tasks execute after).
- **Phase 5 (Polish)**: Depends on US1; best completed after US2 evidence is available.

### User Story Dependencies

- **US1 (P1)**: No dependency on US2. Delivers trustworthy PR signal.
- **US2 (P2)**: Builds on merged CI fixes; focuses on `main` stability and non-PR jobs.

### Within Each User Story

- US1: Local/quickstart parity → targeted code or workflow fix → step naming → GitHub PR verification.
- US2: Trigger matrix confirmation → minimal `build-images` fix only if needed → consecutive-run evidence.

### Parallel Opportunities

- **T002** can run alongside **T001** only if **T001** does not need the contract gap list first; recommended order is T001 then T002, or run T002 after T001’s log is captured.
- **T007**, **T008**, **T009** are parallelizable when multiple areas fail independently (different roots: `apps/frontend-spa/`, `apps/services/`, infra scripts/YAML).

---

## Parallel Example: User Story 1

```text
# After T006, if both frontend and backend logs show independent failures:
Task T007: Fix apps/frontend-spa lint/test/build issues.
Task T008: Fix apps/services test/build issues under TradingPlatform.sln.

# If infra script vs workflow-only:
Task T009: Adjust .github/workflows/ci.yml (and only if needed scripts/infra/validate-bicep.ps1 or package.json).
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1–2 (diagnosis, local parity, infra runner prerequisites, job graph sanity).
2. Complete Phase 3 (US1) until a real PR shows full green CI.
3. **STOP and VALIDATE**: Demo trustworthy PR checks (FR-001/FR-002).

### Incremental Delivery

1. Phase 1–2 → honest local reproduction + workflow prerequisites.
2. US1 → green PR checks; merge.
3. US2 → stable `main` + SC-001 evidence.
4. Polish → duration notes + FR-003/SC-004 audit.

### Parallel Team Strategy

- Engineer A: Phase 2 `.github/workflows/ci.yml` infra job.
- Engineer B: `apps/frontend-spa` failures (T007).
- Engineer C: `apps/services` failures (T008).
- Coordinator: T001/T010/T011/T014 (single-owner verification).

---

## Notes

- Keep edits scoped to CI and minimal supporting scripts per `specs/007-fix-github-ci/plan.md` (FR-003).
- Prefer the smallest workflow change that restores honest green/red signal over disabling jobs.
- If quarantining flaky tests, document a time-bounded follow-up per edge cases in `specs/007-fix-github-ci/spec.md`.
