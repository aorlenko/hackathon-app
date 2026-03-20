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
import { getMyTraderSnapshot, type TraderSnapshotDto } from "./tradingPetsApi";

type Ctx = {
  traderId: string;
  snapshot: TraderSnapshotDto | null;
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
};

const MyPetTraderContext = createContext<Ctx | null>(null);

export const MyPetTraderProvider = () => {
  const auth = useTradingAuth();
  const [traderId, setTraderId] = useState("");
  const [snapshot, setSnapshot] = useState<TraderSnapshotDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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
      setError(e instanceof Error ? e.message : "Could not load pet trader");
      setSnapshot(null);
      setTraderId("");
    } finally {
      setLoading(false);
    }
  }, [auth.accessToken]);

  useEffect(() => {
    setLoading(true);
    void refresh();
  }, [refresh]);

  const value = useMemo(
    () => ({ traderId, snapshot, loading, error, refresh }),
    [traderId, snapshot, loading, error, refresh],
  );

  if (loading) {
    return (
      <div className="trading-pets-page trading-pets-page--centered">
        <p className="trading-pets-loading">Preparing your pet trader…</p>
      </div>
    );
  }

  if (error || !traderId) {
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
      <Outlet />
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
