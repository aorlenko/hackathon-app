# Phase 0 Research: Simplified Real-Time Trading Platform

## Decision 1: Service Topology

- **Decision**: Use three backend services (Market, Trade, Settlement) and one SPA frontend in a monorepo.
- **Rationale**: Meets requirement for clear module boundaries and parallel AI-agent delivery while avoiding over-fragmentation.
- **Alternatives considered**:
  - Single backend service: faster initially but weak service boundaries for parallel implementation.
  - More than three services: adds orchestration overhead with low demo value.

## Decision 2: Hosting Model

- **Decision**: Deploy all runtime apps (3 backend + frontend container) into one Azure Container Apps environment.
- **Rationale**: Simplifies networking, observability, and CI/CD while preserving independent deployability.
- **Alternatives considered**:
  - Separate environments per service: unnecessary complexity for hackathon scope.
  - Frontend on separate hosting product: adds split deployment paths and drift risk.

## Decision 3: Database Ownership Strategy

- **Decision**: Use EF Core migrations with default-schema tables and service-owned table sets.
- **Rationale**: Keeps the persistence model code-first, removes handwritten SQL drift, and avoids schema-specific coupling while preserving clear ownership by context and table naming.
- **Alternatives considered**:
  - Separate database per service: stronger isolation but slower setup/operations.
  - Handwritten SQL scripts: easy to start but drifts from the runtime model and duplicates EF responsibilities.

## Decision 4: Messaging and Workflow

- **Decision**: Use Azure Service Bus topics/subscriptions for asynchronous lifecycle events.
- **Rationale**: Reliable at-least-once delivery, simple pub/sub fit for order-trade-settlement progression, and easy replay/debug.
- **Alternatives considered**:
  - Direct synchronous calls: simpler but tightly coupled and brittle for delayed settlement.
  - Event Grid: less suitable for internal command/event coordination patterns needed here.

## Decision 5: Real-Time UI Update Mechanism

- **Decision**: Use SignalR hub hosted in Market Service as primary real-time gateway to the SPA.
- **Rationale**: Single client connection model with clear group routing for order book, trade stream, and settlement status.
- **Alternatives considered**:
  - Polling-only UI: simpler but lower demo value and weaker latency guarantees.
  - Separate realtime gateway service: adds infrastructure and coordination complexity.

## Decision 6: Authentication

- **Decision**: Use Auth0 for SPA login and JWT bearer token validation in APIs.
- **Rationale**: Meets explicit stack constraint and speeds implementation with proven managed identity flow.
- **Alternatives considered**:
  - Custom identity service: not hackathon-friendly.
  - Azure AD B2C: viable but outside explicit constraint and setup path.

## Decision 7: Telemetry and Performance Validation

- **Decision**: Use OpenTelemetry instrumentation with Application Insights/Azure Monitor for traces, logs, metrics, and alerts.
- **Rationale**: Provides evidence for latency budgets and quick diagnostics during demos.
- **Alternatives considered**:
  - Logs only: insufficient for budget verification.
  - Third-party APM: extra integration cost.

## Decision 8: Configurability for Twist Readiness

- **Decision**: Externalize matching/settlement parameters via configuration and feature flags stored in Key Vault-backed settings.
- **Rationale**: Allows late rule changes without rewriting core modules, directly supporting SC-006.
- **Alternatives considered**:
  - Hardcoded policies: fastest initial coding but high adaptation cost.
  - Dynamic rule engine platform: too heavy for hackathon.

## Decision 9: CI/CD Depth

- **Decision**: Implement lightweight Git-based pipelines with PR validation, image build/push, Bicep validation/deploy, and smoke tests.
- **Rationale**: Balances quality gates with delivery speed and provides repeatable deploy flow for demo confidence.
- **Alternatives considered**:
  - Manual deployment only: faster day 1 but fragile and non-repeatable.
  - Full enterprise release orchestration: excessive setup cost.

## Resolved Clarifications

All technical context clarifications are resolved from user-provided stack constraints and approved specification:
- Runtime stack, hosting, database, messaging, secrets, telemetry, auth, frontend/backend technologies, real-time mechanism, IaC, and CI/CD approach are fixed.
