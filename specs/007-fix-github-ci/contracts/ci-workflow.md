# Contract: CI workflow behavior

**Feature**: 007-fix-github-ci | **Workflow**: `.github/workflows/ci.yml` | **Date**: 2026-03-21

## Purpose

Define the **observable behavior** of continuous integration for this repository so implementation matches [spec.md](../spec.md) (FR-001–FR-004, SC-001–SC-004).

## Triggers

| Event | Branches | Expected outcome |
|--------|-----------|------------------|
| `pull_request` | `main` | All **applicable** jobs required for merge must **pass** for healthy changes (FR-001). |
| `push` | `main` | Same; may additionally run jobs gated off PRs (e.g. image build) when conditions are met. |
| `workflow_dispatch` | default branch | Manual run; same expectations as push where jobs are not skipped. |

## Jobs and obligations

### `infra-validate`

- **Must** run `npm run compose:config` successfully when `docker-compose` / Compose file is valid.
- **Must** run `npm run infra:validate` under PowerShell (`pwsh`) successfully, including `az bicep build` for configured templates and JSON parse of `infra/environments/hackathon/parameters.dev.json`.
- **Failure attribution**: failures map to the named step (`Validate docker compose` or `Validate Bicep templates`).

### `frontend` (conditional)

- **Runs when** `apps/frontend-spa/package.json` exists (`hashFiles` guard).
- **Must** `npm ci`, then lint, test, and build per `package.json` scripts when present (`--if-present` in workflow).
- **Working directory**: `apps/frontend-spa`.

### `backend` (conditional)

- **Runs when** `apps/services/TradingPlatform.sln` exists.
- **Must** restore, build Release, and test the solution with `--no-restore` / `--no-build` as in workflow.

### `build-images` (conditional, non-PR)

- **Not** part of deployment delivery for this feature (FR-003: do not refactor deploy pipelines).
- **Observable rule**: when the job runs, failures must still name the failing step (FR-002). Scope changes to this job are **out** unless needed strictly to fix CI correctness—prefer leaving conditions as-is.

## Secrets and variables

- **PR CI** must **not** require Azure registry secrets for the default merge path (image job skipped on `pull_request`).
- Where OIDC / Azure secrets are referenced, they apply only to jobs whose `if:` evaluates true.

## Success criteria mapping

| Spec ID | Contract check |
|---------|----------------|
| SC-001 | Consecutive successful runs on normal activity after fix. |
| SC-002 | At least one PR shows full green CI before merge. |
| SC-003 | No workflow-level failure without a failed child step/log line. |
| SC-004 | No silent edits to deployment workflows (FR-003). |

## Change policy

- **In scope**: `.github/workflows/ci.yml`, and minimal supporting script/package changes for runner parity.
- **Out of scope**: deployment/release workflows and production promotion unless documented minimal exception (FR-003).
