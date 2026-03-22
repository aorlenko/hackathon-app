declare global {
  interface Window {
    __APP_CONFIG__?: Record<string, string | undefined>;
  }
}

const runtimeConfig =
  typeof window !== "undefined" && window.__APP_CONFIG__
    ? window.__APP_CONFIG__
    : undefined;

const read = (key: string, fallback = ""): string => {
  const runtimeValue = runtimeConfig?.[key];
  if (typeof runtimeValue === "string" && runtimeValue.length > 0) {
    return runtimeValue;
  }

  const value = import.meta.env[key];
  return typeof value === "string" && value.length > 0 ? value : fallback;
};

const marketApiBaseUrl = read("VITE_MARKET_API_BASE_URL", "http://localhost:7001");
const tradeApiBaseUrl = read("VITE_TRADE_API_BASE_URL", "http://localhost:7002");
const settlementApiBaseUrl = read("VITE_SETTLEMENT_API_BASE_URL", "http://localhost:7003");

export const env = {
  marketApiBaseUrl,
  tradeApiBaseUrl,
  settlementApiBaseUrl,
  marketHubUrl: read("VITE_MARKET_HUB_URL", `${marketApiBaseUrl}/hubs/market`),
  auth0Domain: read("VITE_AUTH0_DOMAIN"),
  auth0ClientId: read("VITE_AUTH0_CLIENT_ID"),
  auth0Audience: read("VITE_AUTH0_AUDIENCE"),
  enableDemoAuth: read("VITE_ENABLE_DEMO_AUTH", "true").toLowerCase() !== "false",
};

export const hasAuth0Config = Boolean(env.auth0Domain && env.auth0ClientId);
