# Phase 0 Research: Reliable continuous integration

**Feature**: 007-fix-github-ci | **Date**: 2026-03-21

## 1. Diagnosis-first strategy

**Decision**: Start from the **latest failing GitHub Actions run** (and, if needed, the last five runs) and map each failure to a **single named job and step** before changing anything.

**Rationale**: FR-002 and SC-003 require attributable failures; guessing fixes workflow noise instead of root cause.

**Alternatives considered**: Blindly adding tools to the workflow (rejected—may mask the real failure); disabling jobs (rejected—violates FR-001 unless replaced with an equivalent gate).

## 2. CI job inventory (current `ci.yml`)

**Decision**: Treat **three merge-blocking jobs** for PRs as the integration surface: `infra-validate`, `frontend`, `backend`. The `build-images` job runs only when **not** a pull request and when ACR/Azure secrets are configured; it is still **CI** but must not be conflated with “deployment” work in FR-003—**do not expand** its scope or touch deploy workflows.

**Rationale**: Matches existing workflow structure and spec scope (CI only, deployment pipelines untouched).

**Alternatives considered**: Merging jobs into one (rejected—loses parallelism and blurrier failure attribution).

## 3. Runner prerequisites for `infra-validate`

**Decision**: Ensure the `infra-validate` job provides everything `npm run compose:config` and `npm run infra:validate` require:

- **Docker Compose v2** (`docker compose`) for `compose:config`.
- **PowerShell 7** (`pwsh`) — the Bicep step already sets `shell: pwsh`.
- **Azure CLI** (`az`) with **Bicep** support — `validate-bicep.ps1` fails fast if `az` is missing and runs `az bicep build`.

**Rationale**: Local success on a developer machine with `az` installed does not prove CI has the same path; runner images change over time—**confirm** in the failing log whether `az` or `bicep` is the first error.

**Alternatives considered**:

- **Standalone Bicep CLI** instead of `az bicep` (valid if team wants lighter deps; requires script/workflow change and install step).
- **`azure/cli` action** only for the validate step (keeps version pinned; slightly more YAML).

## 4. Conditional jobs and `needs` (skip behavior)

**Decision**: If any required job is **skipped** (e.g. `if:` on `hashFiles` evaluates false), dependent jobs can **skip** or leave the workflow in a state that is confusing for branch protection—**verify** `needs:` edges against `if:` on `frontend`, `backend`, and `build-images`.

**Rationale**: GitHub Actions skip propagation is a common source of “red” or blocked merges unrelated to code quality.

**Alternatives considered**: `always()`/`if: success() || failure()` hacks (use only if a minimal change is required and documented).

## 5. Frontend and backend failures

**Decision**: For failures in `frontend` or `backend`, reproduce with the same commands as the workflow (working directory `apps/frontend-spa` for SPA; `dotnet restore/build/test` on `TradingPlatform.sln` from repo root).

**Rationale**: Keeps CI fixes aligned with local developer workflow (see `quickstart.md`).

**Alternatives considered**: Skipping tests in CI (rejected—violates constitution and FR-001).

## 6. Flakiness and external deps

**Decision**: Treat **intermittent** failures as in-scope **only** when stabilization is proportionate (retries with cap, ordering, or test isolation). If quarantine is unavoidable, spec edge case requires a **time-bounded follow-up** documented in PR/delivery notes.

**Rationale**: Matches spec edge case on flaky checks.

**Alternatives considered**: Permanent quarantine without follow-up (rejected).

## 7. Performance evidence (PRF)

**Decision**: Record **median wall-clock** for a full successful workflow before and after changes using GitHub’s run summary; if duration increases materially, document rationale per PRF-002.

**Rationale**: Satisfies constitution performance gate for this automation-centric feature.

**Alternatives considered**: No measurement (rejected—non-compliant with constitution IV).

## 8. Explicit non-goals (FR-003)

**Decision**: Do **not** modify `.github/workflows/deploy-hackathon.yml`, `scripts/infra/deploy-hackathon.ps1`, or related promotion automation as part of this plan. If CI truly cannot pass without a deploy-file one-line change, treat as **documented minimal exception** in the delivery PR.

**Rationale**: Spec out-of-scope boundary.

**Alternatives considered**: None.

## 9. Failure mapping (diagnosis baseline, 2026-03-21)

**Note**: No `gh`/API access from the implementation environment to read the latest Actions run. The prior `ci.yml` ran `infra-validate` on `ubuntu-latest` with:

- `Validate Bicep templates` using `shell: pwsh` while **PowerShell 7 is not pre-installed** on default Ubuntu runners.
- `npm run infra:validate` invoking `validate-bicep.ps1`, which requires **`az`** while **Azure CLI is not pre-installed** on those runners.

The first hard failure is therefore expected on job **`infra-validate`**, step **`Validate Bicep templates`** (or the shell bootstrap immediately before the script runs), until those tools are installed on the job.

## 10. Contract vs `ci.yml` gaps closed (2026-03-21)

| Gap | Resolution |
|-----|------------|
| Contract: `frontend` / `backend` run only when path guards match | **Not implemented at job level**: GitHub Actions does **not** allow `hashFiles()` in `jobs.<id>.if` (parser error: unrecognized function). Those jobs stay **unconditional** while paths exist in this repo; use step-level `if` + `hashFiles` only if skip-on-missing-path is required later. |
| Contract: infra steps need Compose v2, `pwsh`, and `az bicep` | Added an explicit **Install Azure CLI and PowerShell 7** step before **Validate Bicep templates**; Compose remains **Validate docker compose** via `docker compose`. |
| FR-002: image matrix failures should name the service | Renamed the matrix step to **Build and push `${{ matrix.name }}` image**. |
