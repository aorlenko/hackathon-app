import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { Outlet } from "react-router-dom";
import { useTradingAuth } from "../auth/AuthProvider";
import { emitPetTraderSnapshotUpdated } from "./petTraderSnapshotEvents";
import { getMyTraderSnapshot, type TraderSnapshotDto } from "./tradingPetsApi";
import { TradingPetToastStack, type PetTradingToast } from "./TradingPetToastStack";
import {
  useTraderNotificationToasts,
  type TraderToastPayload,
} from "./useTraderNotificationToasts";
import { useTradingPetsRealtime } from "./useTradingPetsRealtime";

type Ctx = {
  traderId: string;
  snapshot: TraderSnapshotDto | null;
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  /** Bumps on each SignalR-driven refresh so panels can reload listings/inventory. */
  hubInvalidateSeq: number;
  showToast: (payload: TraderToastPayload) => void;
};

type HubSyncProps = {
  bumpHubInvalidate: () => void;
  toasts: PetTradingToast[];
  dismissToast: (toastInstanceId: string) => void;
  onTraderNotificationsAdded: () => Promise<void>;
};

function MyPetTraderHubSync({
  bumpHubInvalidate,
  toasts,
  dismissToast,
  onTraderNotificationsAdded,
}: HubSyncProps) {
  const auth = useTradingAuth();
  const { traderId, refresh } = useMyPetTrader();

  const onHubRefresh = useCallback(async () => {
    try {
      await refresh();
    } catch {
      /* snapshot optional for cross-user listing updates */
    }
    bumpHubInvalidate();
  }, [refresh, bumpHubInvalidate]);

  useTradingPetsRealtime({
    traderId,
    accessToken: auth.accessToken,
    onRefreshSnapshot: onHubRefresh,
    onTraderNotificationsAdded,
  });

  return (
    <>
      <TradingPetToastStack toasts={toasts} onDismiss={dismissToast} />
      <Outlet />
    </>
  );
}

const MyPetTraderContext = createContext<Ctx | null>(null);

export const MyPetTraderProvider = () => {
  const auth = useTradingAuth();
  const [traderId, setTraderId] = useState("");
  const [snapshot, setSnapshot] = useState<TraderSnapshotDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [hubInvalidateSeq, setHubInvalidateSeq] = useState(0);
  const bumpHubInvalidate = useCallback(() => setHubInvalidateSeq((n) => n + 1), []);
  const { toasts, dismissToast, onTraderNotificationsAdded, showToast } =
    useTraderNotificationToasts(traderId, auth.accessToken);

  const refresh = useCallback(async () => {
    if (!auth.accessToken) {
      setLoading(false);
      return;
    }
    setError(null);
    try {
      const s = await getMyTraderSnapshot(auth.accessToken);
      setSnapshot(s);
      setTraderId(s.traderId);
    } catch (e) {
      const message = e instanceof Error ? e.message : "Could not load pet trader";
      setError(message);
      setSnapshot((prev) => (prev?.traderId ? prev : null));
      setTraderId((prev) => (prev || ""));
    } finally {
      setLoading(false);
    }
  }, [auth.accessToken]);

  useEffect(() => {
    setLoading(true);
    void refresh();
  }, [refresh]);

  useEffect(() => {
    if (snapshot?.traderId) {
      emitPetTraderSnapshotUpdated(snapshot);
    }
  }, [snapshot]);

  const value = useMemo(
    () => ({ traderId, snapshot, loading, error, refresh, hubInvalidateSeq, showToast }),
    [traderId, snapshot, loading, error, refresh, hubInvalidateSeq, showToast],
  );

  if (loading) {
    return (
      <div className="trading-pets-page trading-pets-page--centered">
        <p className="trading-pets-loading">Preparing your pet trader…</p>
      </div>
    );
  }

  if (!traderId) {
    return (
      <div className="trading-pets-page trading-pets-page--centered">
        <p className="trading-pets-error">{error ?? "Unable to resolve pet trader."}</p>
        <button type="button" className="primary-button" onClick={() => void refresh()}>
          Retry
        </button>
      </div>
    );
  }

  return (
    <MyPetTraderContext.Provider value={value}>
      <MyPetTraderHubSync
        bumpHubInvalidate={bumpHubInvalidate}
        toasts={toasts}
        dismissToast={dismissToast}
        onTraderNotificationsAdded={onTraderNotificationsAdded}
      />
    </MyPetTraderContext.Provider>
  );
};

export const useMyPetTrader = (): Ctx => {
  const v = useContext(MyPetTraderContext);
  if (!v) {
    throw new Error("useMyPetTrader must be used inside MyPetTraderProvider");
  }
  return v;
};
