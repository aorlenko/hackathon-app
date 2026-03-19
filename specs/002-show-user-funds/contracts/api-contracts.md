# API Contracts: Visible User Funds

## Overview

This feature keeps account bootstrap behavior intact and adds a dedicated account snapshot query for the always-visible funds display.

## 1. Existing Endpoint: Bootstrap Demo Account

### `POST /api/accounts/me/bootstrap`

- **Purpose**: Ensure the authenticated user has a demo account and return the initial account snapshot after login/bootstrap.
- **Auth**: Required.
- **Request body**:

```json
{
  "displayName": "Demo Trader",
  "email": "trader@example.com"
}
```

- **Response `200 OK`**:

```json
{
  "userId": "user-1",
  "displayName": "Demo Trader",
  "email": "trader@example.com",
  "cashAvailable": 10000.0,
  "holdings": [
    {
      "symbol": "MSFT",
      "quantity": 25
    }
  ]
}
```

- **Notes**:
  - Existing contract already exists in the repo and remains part of the login/bootstrap path.
  - The frontend should not use this endpoint for ordinary retry or refresh behavior once the account exists.

## 2. New Endpoint: Read Current Account Snapshot

### `GET /api/accounts/me`

- **Purpose**: Return the latest confirmed account snapshot used by the header funds display.
- **Auth**: Required.
- **Request body**: None.
- **Response `200 OK`**:

```json
{
  "userId": "user-1",
  "displayName": "Demo Trader",
  "email": "trader@example.com",
  "cashAvailable": 9950.0,
  "holdings": [
    {
      "symbol": "MSFT",
      "quantity": 26
    }
  ]
}
```

- **Error cases**:
  - `401 Unauthorized`: no authenticated user context.
  - `404 Not Found` or equivalent domain error: account has not been bootstrapped yet.
  - `5xx`: temporary retrieval failure; frontend transitions to `unavailable` while preserving the last confirmed amount if one exists.

## 3. API Behavior Rules

- Returned `cashAvailable` must represent the latest confirmed funds amount persisted for the active account.
- The endpoint must never synthesize optimistic client-side values.
- Retry-safe account refreshes must use `GET /api/accounts/me`, not repeated bootstrap calls.
- Rejected or cancelled trade attempts must not affect the returned amount.

## 4. Compatibility Notes

- The feature is intentionally additive:
  - existing bootstrap contract remains valid
  - new query endpoint is introduced without changing route shape for current consumers
- The response shape intentionally aligns with the existing `DemoAccountDto` so frontend account state can be hydrated from either bootstrap or refresh responses without separate mapping models.
