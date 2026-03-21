import { Link } from "react-router-dom";
import { useTradingAuth } from "../auth/AuthProvider";
import { useMyPetTrader } from "./MyPetTraderContext";
import { PrimaryMarketPanel } from "./PrimaryMarketPanel";
import { SecondaryMarketPanel } from "./SecondaryMarketPanel";

export const TraderWorkspacePage = () => {
  const auth = useTradingAuth();
  const { traderId, snapshot, error: traderError, refresh, hubInvalidateSeq } = useMyPetTrader();

  return (
    <div className="trading-pets-workspace">
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
            reloadToken={hubInvalidateSeq}
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
            reloadToken={hubInvalidateSeq}
            inventory={snapshot?.pets ?? []}
            onChanged={() => void refresh()}
          />
        </section>
      </div>
    </div>
  );
};
