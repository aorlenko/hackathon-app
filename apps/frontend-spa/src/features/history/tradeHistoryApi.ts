import { env } from "../../config/env";
import type { TradeRecord } from "../../contracts/trading";
import { createHeaders, fetchJson } from "../../lib/http";

export const getUserTradeHistory = (userId: string, accessToken?: string) =>
  fetchJson<TradeRecord[]>(
    `${env.tradeApiBaseUrl}/api/users/${encodeURIComponent(userId)}/trades`,
    {
      headers: createHeaders(accessToken),
    },
  );
