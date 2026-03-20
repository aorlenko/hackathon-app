import type {
  AccountSnapshot,
  BootstrapDemoAccountRequest,
} from "../../contracts/trading";
import { env } from "../../config/env";
import { createHeaders, fetchJson } from "../../lib/http";
import { getCurrentAccountSnapshot } from "../account/accountApi";

export const bootstrapDemoAccount = (
  request: BootstrapDemoAccountRequest,
  accessToken?: string,
) =>
  fetchJson<AccountSnapshot>(`${env.marketApiBaseUrl}/api/accounts/me/bootstrap`, {
    method: "POST",
    headers: createHeaders(accessToken),
    body: JSON.stringify(request),
  });

export const refreshAuthenticatedAccount = (accessToken?: string) =>
  getCurrentAccountSnapshot(accessToken);
