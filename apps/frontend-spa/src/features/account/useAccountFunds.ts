import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import type {
  AccountSnapshot,
  FundsDisplayState,
} from "../../contracts/trading";
import { createMarketHubClient, type MarketHubClient } from "../realtime/marketHubClient";
import { refreshAccountSnapshot } from "./accountApi";

export const ACCOUNT_FUNDS_REFRESH_EVENT = "trading:account-funds-refresh";

const fundsFormatter = new Intl.NumberFormat("en-US", {
  style: "currency",
  currency: "USD",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const toErrorMessage = (error: unknown) =>
  error instanceof Error ? error.message : "Funds are temporarily unavailable.";

export const formatFundsAmount = (cashAvailable: number) =>
  fundsFormatter.format(cashAvailable);

export interface UseAccountFundsOptions {
  accessToken?: string;
  client?: MarketHubClient;
  initialSnapshot?: AccountSnapshot | null;
  isAuthenticated: boolean;
  userId: string | null;
}

export const useAccountFunds = ({
  accessToken,
  client,
  initialSnapshot,
  isAuthenticated,
  userId,
}: UseAccountFundsOptions) => {
  const canAccessProtectedResources = Boolean(isAuthenticated && userId && accessToken);
  const [snapshot, setSnapshot] = useState<AccountSnapshot | null>(
    initialSnapshot ?? null,
  );
  const [state, setState] = useState<FundsDisplayState>(
    isAuthenticated && initialSnapshot ? "confirmed" : "loading",
  );
  const [error, setError] = useState("");
  const snapshotRef = useRef<AccountSnapshot | null>(initialSnapshot ?? null);
  const latestUserIdRef = useRef<string | null>(userId);
  const ownsClient = !client;
  const realtimeClient = useMemo(
    () =>
      client ??
      (canAccessProtectedResources
        ? createMarketHubClient(async () => accessToken)
        : undefined),
    [accessToken, canAccessProtectedResources, client],
  );

  useEffect(() => {
    snapshotRef.current = snapshot;
  }, [snapshot]);

  useEffect(() => {
    latestUserIdRef.current = userId;
  }, [userId]);

  useEffect(() => {
    if (!isAuthenticated || !userId) {
      setSnapshot(null);
      setState("loading");
      setError("");
      return;
    }

    if (!accessToken) {
      setState(initialSnapshot ? "confirmed" : "loading");
      setError("");
      return;
    }

    if (initialSnapshot && initialSnapshot.userId === userId) {
      setSnapshot((current) =>
        current?.userId === userId ? current : initialSnapshot,
      );
      setState("confirmed");
      setError("");
      return;
    }

    setSnapshot((current) => (current?.userId === userId ? current : null));
    setState("loading");
    setError("");
  }, [accessToken, initialSnapshot, isAuthenticated, userId]);

  const refresh = useCallback(async () => {
    if (!canAccessProtectedResources) {
      return null;
    }

    setState(snapshotRef.current ? "updating" : "loading");

    try {
      const nextSnapshot = await refreshAccountSnapshot(accessToken);
      if (latestUserIdRef.current !== nextSnapshot.userId) {
        return nextSnapshot;
      }

      setSnapshot(nextSnapshot);
      setState("confirmed");
      setError("");
      return nextSnapshot;
    } catch (refreshError: unknown) {
      setState("unavailable");
      setError(toErrorMessage(refreshError));
      return null;
    }
  }, [accessToken, canAccessProtectedResources]);

  useEffect(() => {
    if (!canAccessProtectedResources || initialSnapshot) {
      return;
    }

    void refresh();
  }, [canAccessProtectedResources, initialSnapshot, refresh]);

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    const handleRefreshRequested = () => {
      void refresh();
    };

    window.addEventListener(ACCOUNT_FUNDS_REFRESH_EVENT, handleRefreshRequested);
    return () =>
      window.removeEventListener(
        ACCOUNT_FUNDS_REFRESH_EVENT,
        handleRefreshRequested,
      );
  }, [refresh]);

  useEffect(() => {
    if (!realtimeClient || !canAccessProtectedResources || !userId) {
      return;
    }

    let active = true;
    let started = false;

    const fundsUnsubscribe = realtimeClient.onFundsUpdated((message) => {
      if (!active || message.payload.userId !== userId) {
        return;
      }

      if (!snapshotRef.current) {
        void refresh();
        return;
      }

      setSnapshot({
        ...snapshotRef.current,
        cashAvailable: message.payload.cashAvailable,
      });
      setState("confirmed");
      setError("");
    });
    const reconnectUnsubscribe = realtimeClient.onReconnected(async () => {
      if (!active) {
        return;
      }

      setState(snapshotRef.current ? "updating" : "loading");
      await refresh();
    });

    const connect = async () => {
      try {
        await realtimeClient.start();
        started = true;
        if (!active && ownsClient) {
          await realtimeClient.stop();
        }
      } catch {
        if (active && !snapshotRef.current) {
          setState("unavailable");
          setError("Live funds updates are temporarily unavailable.");
        }
      }
    };

    void connect();

    return () => {
      active = false;
      fundsUnsubscribe();
      reconnectUnsubscribe();

      if (ownsClient && started) {
        void realtimeClient.stop();
      }
    };
  }, [canAccessProtectedResources, ownsClient, realtimeClient, refresh, userId]);

  return useMemo(
    () => ({
      error,
      formattedFunds: snapshot ? formatFundsAmount(snapshot.cashAvailable) : null,
      refresh,
      snapshot,
      state,
    }),
    [error, refresh, snapshot, state],
  );
};
