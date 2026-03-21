import { useCallback, useState } from "react";
import { Link } from "react-router-dom";
import { useTradingAuth } from "../auth/AuthProvider";
import { useMyPetTrader } from "./MyPetTraderContext";
import { PrimaryMarketPanel } from "./PrimaryMarketPanel";
import { SecondaryMarketPanel } from "./SecondaryMarketPanel";
import { TradingPetToastStack } from "./TradingPetToastStack";
import { useTraderNotificationToasts } from "./useTraderNotificationToasts";
import { useTradingPetsRealtime } from "./useTradingPetsRealtime";

export const TraderWorkspacePage = () => {
  const auth = useTradingAuth();
  const { traderId, snapshot, error: traderError, refresh } = useMyPetTrader();
  const [panelTick, setPanelTick] = useState(0);

  const { toasts, dismissToast, onTraderNotificationsAdded } = useTraderNotificationToasts(
    traderId,
    auth.accessToken,
  );

  const onRealtimeInvalidate = useCallback(async () => {
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
    onTraderNotificationsAdded,
  });

  return (
    <div className="trading-pets-workspace">
      <TradingPetToastStack toasts={toasts} onDismiss={dismissToast} />

      <header className="trading-pets-page__header">
        <h1>Pet marketplace workspace</h1>
        <p className="muted trading-pets-page__intro trading-pets-page__intro--full">
          Buy new pets from primary supply and trade on the resale marketplace. Your pet trader balances (available,
          locked, portfolio) show in the <strong>header box next to your account</strong>. Your pets live on{" "}
          <Link to="/pets/my-pets" className="trading-pets-text-link">
            My pets
          </Link>
          . To sell a pet, pick it from your inventory under <strong>Offer a pet for sale</strong> below.
        </p>
      </header>

      {traderError && snapshot ? (
        <p className="trading-pets-workspace__banner" role="alert">
          Could not refresh your latest balances and inventory ({traderError}). Figures in the header summary are from
          your last successful load.{" "}
          <button type="button" className="trading-pets-inline-action" onClick={() => void refresh()}>
            Try again
          </button>
        </p>
      ) : null}

      <div className="trading-pets-grid trading-pets-grid--workspace-pair">
        <section
          className="trading-pets-workspace-region"
          role="region"
          aria-labelledby="trading-region-primary-title"
        >
          <PrimaryMarketPanel
            traderId={traderId}
            accessToken={auth.accessToken}
            reloadToken={panelTick}
            onPurchased={() => void refresh()}
          />
        </section>

        <section
          className="trading-pets-workspace-region"
          role="region"
          aria-labelledby="trading-region-secondary-title"
        >
          <SecondaryMarketPanel
            traderId={traderId}
            accessToken={auth.accessToken}
            reloadToken={panelTick}
            inventory={snapshot?.pets ?? []}
            onChanged={() => void refresh()}
          />
        </section>
      </div>
    </div>
  );
};
