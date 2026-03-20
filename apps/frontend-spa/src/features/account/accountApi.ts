import type {
  AccountIdentity,
  AccountSnapshot,
  ResolveAccountsRequest,
} from "../../contracts/trading";
import { env } from "../../config/env";
import { createHeaders, fetchJson } from "../../lib/http";

export const getCurrentAccountSnapshot = (accessToken?: string) =>
  fetchJson<AccountSnapshot>(`${env.marketApiBaseUrl}/api/accounts/me`, {
    headers: createHeaders(accessToken),
  });

export const refreshAccountSnapshot = (accessToken?: string) =>
  getCurrentAccountSnapshot(accessToken);

export const resolveAccountIdentities = (
  userIds: string[],
  accessToken?: string,
) => {
  const request: ResolveAccountsRequest = { userIds };

  return fetchJson<AccountIdentity[]>(`${env.marketApiBaseUrl}/api/accounts/resolve`, {
    method: "POST",
    headers: createHeaders(accessToken),
    body: JSON.stringify(request),
  });
};
