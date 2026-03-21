# Data Model: CI configuration (conceptual)

**Feature**: 007-fix-github-ci | **Date**: 2026-03-21

This feature does not introduce database entities. The “model” is the **automation configuration** and how it maps to spec requirements.

## Entity: Workflow

| Field | Description |
|--------|-------------|
| `name` | Human-readable workflow title (e.g. `ci`). |
| `on` | Triggers: `pull_request` / `push` to `main`, `workflow_dispatch`. |
| `permissions` | Token scopes (e.g. `contents: read`, `id-token: write` for OIDC consumers). |

**Validation / rules**

- Must satisfy FR-001 (success for healthy changes) and FR-002 (clear failure attribution).
- FR-003: no coupling to deployment workflows for delivery.

## Entity: Job

| Field | Description |
|--------|-------------|
| `id` | Machine id: `infra-validate`, `frontend`, `backend`, `build-images`. |
| `runner` | e.g. `ubuntu-latest`. |
| `if` | Optional condition (e.g. path existence via `hashFiles`). |
| `needs` | Upstream jobs that must complete (affects skip propagation). |
| `defaults.run.working-directory` | SPA job uses `apps/frontend-spa`. |

**Relationships**

- `build-images` **needs** `infra-validate`, `frontend`, `backend` today—changes to skip logic on upstream jobs affect downstream scheduling.
- PRs typically **exclude** `build-images` via `github.event_name != 'pull_request'`.

## Entity: Step

| Field | Description |
|--------|-------------|
| `name` | Shown in logs (primary FR-002 signal). |
| `uses` / `run` | Action reference or shell command. |
| `shell` | e.g. `pwsh` for Bicep validation step. |

**Notable step groups**

- **Infra**: checkout → Node setup → `npm run compose:config` → `npm run infra:validate` (PowerShell + `validate-bicep.ps1`).
- **Frontend**: checkout → Node + cache → `npm ci` → lint → test → build.
- **Backend**: checkout → .NET setup → restore → build → test.

## Entity: Repository script (invoked by CI)

| Name | Role |
|------|------|
| `validate-bicep.ps1` | Requires `az`; runs `az bicep build` on listed `.bicep` files; validates `parameters.dev.json` parses as JSON. |

## State transitions

Not applicable (no long-lived workflow state in a datastore). **Run outcome** is the only state: `success` | `failure` | `cancelled` | `skipped`, with SC-001/SC-003 requiring **success** for healthy inputs and **named failed steps** on failure.
