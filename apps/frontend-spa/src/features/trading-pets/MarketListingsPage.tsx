import { useCallback, useEffect, useState } from "react";
import { useTradingAuth } from "../auth/AuthProvider";
import { getMarketListings, type MarketListingDto } from "./tradingPetsApi";
import { Link } from "react-router-dom";
import { useMyPetTrader } from "./MyPetTraderContext";
import { listingSellerClause } from "./listingSellerLabel";
import { useTradingPetsRealtime } from "./useTradingPetsRealtime";

export const MarketListingsPage = () => {
  const auth = useTradingAuth();
  const { traderId } = useMyPetTrader();
  const [rows, setRows] = useState<MarketListingDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);
    try {
      setRows(await getMarketListings(auth.accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load market");
    }
  }, [auth.accessToken]);

  useEffect(() => {
    void load();
  }, [load]);

  useTradingPetsRealtime({
    traderId,
    accessToken: auth.accessToken,
    onRefreshSnapshot: load,
  });

  return (
    <div className="trading-pets-page">
      <header className="trading-pets-page__header">
        <h1>Pet listings</h1>
        <p className="muted">
          Resale offers: pets listed by traders at an asking price. Newest first, with breed supply and recent trade
          context.
        </p>
      </header>
      <ul className="trading-pets-list trading-pets-list--market">
        {rows.length === 0 && !error ? (
          <li className="trading-pets-empty">No open listings yet. Buy a pet on the trading page, then create a listing.</li>
        ) : null}
        {rows.map((l) => {
          const sellerClause = listingSellerClause(
            l.sellerTraderId,
            l.sellerDisplayName,
            l.sellerEmail,
            traderId,
          );
          return (
          <li key={l.listingId}>
            <div>
              <strong>{l.breedName}</strong> · ${l.askingPrice.toFixed(2)}
              {sellerClause ? <> · {sellerClause}</> : null}
            </div>
            <div className="muted small">
              Listed {new Date(l.createdAt).toLocaleString()} · Pet{" "}
              <Link to={`/pets/analysis/${l.petId}`}>{l.petId.slice(0, 8)}…</Link>
            </div>
            <div className="muted small">
              Recent trade (breed):{" "}
              {l.recentTradePriceForBreed == null
                ? "—"
                : `$${l.recentTradePriceForBreed.toFixed(2)}`}{" "}
              · Remaining new supply: {l.remainingNewSupplyForBreed}
            </div>
          </li>
        );
        })}
      </ul>
      {error ? <p className="trading-pets-error">{error}</p> : null}
    </div>
  );
};
