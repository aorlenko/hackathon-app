import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";
import { useTradingAuth } from "../auth/AuthProvider";
import { PET_TRADER_SNAPSHOT_UPDATED } from "./petTraderSnapshotEvents";
import type { TraderSnapshotDto } from "./tradingPetsApi";

/**
 * Pet trader balances inside {@link auth-panel__summary} (pet routes only).
 * Synced via {@link emitPetTraderSnapshotUpdated} from MyPetTraderProvider.
 */
export const PetTraderAuthSummaryMetrics = () => {
  const auth = useTradingAuth();
  const location = useLocation();
  const [snapshot, setSnapshot] = useState<TraderSnapshotDto | null>(null);

  const inPetRealm =
    auth.isAuthenticated && location.pathname.startsWith("/pets");

  useEffect(() => {
    if (!inPetRealm) {
      setSnapshot(null);
      return;
    }

    const onUpdate = (e: Event) => {
      const ce = e as CustomEvent<TraderSnapshotDto>;
      if (ce.detail?.traderId) {
        setSnapshot(ce.detail);
      }
    };

    window.addEventListener(PET_TRADER_SNAPSHOT_UPDATED, onUpdate);
    return () => window.removeEventListener(PET_TRADER_SNAPSHOT_UPDATED, onUpdate);
  }, [inPetRealm]);

  if (!inPetRealm || !snapshot?.traderId) {
    return null;
  }

  const fmt = (n: number) =>
    `US$${n.toLocaleString("en-US", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })}`;

  return (
    <div
      className="auth-panel__pet-metrics"
      role="group"
      aria-label="Pet trader balances"
    >
      <div className="auth-panel__pet-metric">
        <span className="auth-panel__pet-label">Available</span>
        <span className="auth-panel__pet-value">{fmt(snapshot.availableCash)}</span>
      </div>
      <div className="auth-panel__pet-metric">
        <span className="auth-panel__pet-label">Locked</span>
        <span className="auth-panel__pet-value">{fmt(snapshot.lockedCash)}</span>
      </div>
      <div className="auth-panel__pet-metric">
        <span className="auth-panel__pet-label">Portfolio</span>
        <span className="auth-panel__pet-value">{fmt(snapshot.portfolioTotal)}</span>
      </div>
    </div>
  );
};
