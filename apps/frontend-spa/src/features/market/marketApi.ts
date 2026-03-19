import { env } from "../../config/env";
import type {
  MarketSummary,
  OrderBookSnapshot,
  PlaceOrderRequest,
  PlaceOrderResponse,
  TradeRecord,
} from "../../contracts/trading";
import { createHeaders, fetchJson } from "../../lib/http";

export const getMarkets = (accessToken?: string) =>
  fetchJson<MarketSummary[]>(`${env.marketApiBaseUrl}/api/markets`, {
    headers: createHeaders(accessToken),
  });

export const getOrderBook = (symbol: string, accessToken?: string) =>
  fetchJson<OrderBookSnapshot>(
    `${env.marketApiBaseUrl}/api/markets/${encodeURIComponent(symbol)}/order-book`,
    {
      headers: createHeaders(accessToken),
    },
  );

export const placeOrder = (
  request: PlaceOrderRequest,
  accessToken?: string,
) =>
  fetchJson<PlaceOrderResponse>(`${env.marketApiBaseUrl}/api/orders`, {
    method: "POST",
    headers: createHeaders(accessToken),
    body: JSON.stringify(request),
  });

export const getRecentTrades = (symbol: string, accessToken?: string) =>
  fetchJson<TradeRecord[]>(
    `${env.tradeApiBaseUrl}/api/trades?symbol=${encodeURIComponent(symbol)}`,
    {
      headers: createHeaders(accessToken),
    },
  );
