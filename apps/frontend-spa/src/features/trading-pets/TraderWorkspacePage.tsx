import { useCallback, useState } from "react";
import { useTradingAuth } from "../auth/AuthProvider";
import { useMyPetTrader } from "./MyPetTraderContext";
import { NotificationsPanel } from "./NotificationsPanel";
import { PrimaryMarketPanel } from "./PrimaryMarketPanel";
import { SecondaryMarketPanel } from "./SecondaryMarketPanel";
import { useTradingPetsRealtime } from "./useTradingPetsRealtime";

export const TraderWorkspacePage = () => {
  const auth = useTradingAuth();
  const { traderId, snapshot, refresh } = useMyPetTrader();
  const [panelTick, setPanelTick] = useState(0);

  const onRealtimeInvalidate = useCallback(async () => {
    // Bump panels first so listings/breeds/notifications refetch even if snapshot refresh hangs or fails.
    setPanelTick((n) => n + 1);
    try {
      await refresh();
    } catch {
      /* snapshot optional for cross-user listing updates */
    }
  }, [refresh]);

  useTradingPetsRealtime({
    traderId,
    accessToken: auth.accessToken,
    onRefreshSnapshot: onRealtimeInvalidate,
  });

  return (
    <div className="trading-pets-workspace">
      <header className="trading-pets-page__header">
        <h1>Pet trading</h1>
        <p className="muted">
          Buy from primary supply, list on the secondary market, and track bids — all tied to your signed-in
          account.
        </p>
      </header>

      {snapshot ? (
        <section className="trading-pets-summary">
          <div>
            <div className="muted small">Available cash</div>
            <strong>${snapshot.availableCash.toFixed(2)}</strong>
          </div>
          <div>
            <div className="muted small">Locked cash</div>
            <strong>${snapshot.lockedCash.toFixed(2)}</strong>
          </div>
          <div>
            <div className="muted small">Portfolio total</div>
            <strong>${snapshot.portfolioTotal.toFixed(2)}</strong>
          </div>
        </section>
      ) : null}

      <div className="trading-pets-grid">
        <PrimaryMarketPanel
          traderId={traderId}
          accessToken={auth.accessToken}
          reloadToken={panelTick}
          onPurchased={() => void refresh()}
        />
        <SecondaryMarketPanel
          traderId={traderId}
          accessToken={auth.accessToken}
          reloadToken={panelTick}
          inventory={snapshot?.pets ?? []}
          onChanged={() => void refresh()}
        />
        <section className="trading-pets-card">
          <header className="trading-pets-card__header">
            <h2>Your pets</h2>
            <p className="muted small">Inventory for your account.</p>
          </header>
          <ul className="trading-pets-list">
            {(snapshot?.pets ?? []).length === 0 ? (
              <li className="trading-pets-empty">No pets yet — buy from the primary market.</li>
            ) : (
              (snapshot?.pets ?? []).map((p) => (
                <li key={p.id}>
                  <div>
                    <strong>{p.breedName}</strong> · age {p.ageYears.toFixed(2)}y · health{" "}
                    {p.health.toFixed(0)}% · desirability {p.currentDesirability}
                  </div>
                  <div className="muted small">
                    Intrinsic ${p.intrinsicValue.toFixed(2)} · maintenance ${p.maintenanceCost.toFixed(2)}{" "}
                    {p.isExpired ? "· expired" : ""}
                  </div>
                </li>
              ))
            )}
          </ul>
        </section>
        <NotificationsPanel
          traderId={traderId}
          accessToken={auth.accessToken}
          reloadToken={panelTick}
        />
      </div>
    </div>
  );
};
