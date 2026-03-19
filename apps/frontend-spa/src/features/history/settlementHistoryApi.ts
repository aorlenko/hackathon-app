import { env } from "../../config/env";
import type { SettlementRecord } from "../../contracts/trading";
import { createHeaders, fetchJson } from "../../lib/http";

export const getUserSettlementHistory = (
  userId: string,
  accessToken?: string,
) =>
  fetchJson<SettlementRecord[]>(
    `${env.settlementApiBaseUrl}/api/users/${encodeURIComponent(userId)}/settlements`,
    {
      headers: createHeaders(accessToken),
    },
  );
