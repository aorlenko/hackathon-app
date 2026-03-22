import { useCallback } from "react";
import { Link } from "react-router-dom";
import { useTradingAuth } from "../auth/AuthProvider";
import { useMyPetTrader } from "./MyPetTraderContext";
import { PrimaryMarketPanel, type PrimaryPurchaseSummary } from "./PrimaryMarketPanel";

export const PrimarySupplyMarketPage = () => {
  const auth = useTradingAuth();
  const { traderId, snapshot, error: traderError, refresh, hubInvalidateSeq, showToast } =
    useMyPetTrader();
  const handlePurchased = useCallback(
    async ({ breedName, quantity, totalPrice }: PrimaryPurchaseSummary) => {
      showToast({
        dedupeKey: `primary-purchase-${traderId}-${Date.now()}`,
        title: "Primary purchase completed",
        body: `${quantity === 1 ? "1 pet" : `${quantity} pets`} · ${breedName} · $${totalPrice.toFixed(2)}`,
        variant: "trade",
      });
      await refresh();
    },
    [refresh, showToast, traderId],
  );

  return (
    <div className="trading-pets-workspace">
      <header className="trading-pets-page__header">
        <h1>Primary supply market</h1>
        <p className="muted trading-pets-page__intro trading-pets-page__intro--full">
          Buy new pets from primary supply at each breed&apos;s retail price. Your pet trader balances (available, locked,
          portfolio) show in the <strong>header box next to your account</strong>. Owned pets are on{" "}
          <Link to="/pets/my-pets" className="trading-pets-text-link">
            My pets
          </Link>
          . For peer-to-peer resale, open{" "}
          <Link to="/pets/resale" className="trading-pets-text-link">
            Resale marketplace
          </Link>
          .
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

      <section className="trading-pets-workspace-region" role="region" aria-labelledby="trading-region-primary-title">
        <PrimaryMarketPanel
          traderId={traderId}
          accessToken={auth.accessToken}
          reloadToken={hubInvalidateSeq}
          onPurchased={handlePurchased}
        />
      </section>
    </div>
  );
};
