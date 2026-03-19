import type {
  BootstrapDemoAccountRequest,
  DemoAccount,
} from "../../contracts/trading";
import { env } from "../../config/env";
import { createHeaders, fetchJson } from "../../lib/http";

export const bootstrapDemoAccount = (
  request: BootstrapDemoAccountRequest,
  accessToken?: string,
) =>
  fetchJson<DemoAccount>(`${env.marketApiBaseUrl}/api/accounts/me/bootstrap`, {
    method: "POST",
    headers: createHeaders(accessToken),
    body: JSON.stringify(request),
  });
