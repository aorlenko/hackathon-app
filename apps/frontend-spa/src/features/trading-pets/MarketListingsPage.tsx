import { useEffect, useState } from "react";
import { useTradingAuth } from "../auth/AuthProvider";
import { getMarketListings, type MarketListingDto } from "./tradingPetsApi";
import { Link } from "react-router-dom";

export const MarketListingsPage = () => {
  const auth = useTradingAuth();
  const [rows, setRows] = useState<MarketListingDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setError(null);
    try {
      setRows(await getMarketListings(auth.accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load market");
    }
  };

  useEffect(() => {
    void load();
  }, [auth.accessToken]);

  return (
    <div className="trading-pets-page">
      <header className="trading-pets-page__header">
        <h1>Pet listings</h1>
        <p className="muted">Newest first, with breed context for supply and recent trades.</p>
        <button type="button" className="secondary-button" onClick={() => void load()}>
          Refresh
        </button>
      </header>
      <ul className="trading-pets-list trading-pets-list--market">
        {rows.length === 0 && !error ? (
          <li className="trading-pets-empty">No open listings yet. Buy a pet on the trading page, then create a listing.</li>
        ) : null}
        {rows.map((l) => (
          <li key={l.listingId}>
            <div>
              <strong>{l.breedName}</strong> · ${l.askingPrice.toFixed(2)} · {l.sellerDisplayName}
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
        ))}
      </ul>
      {error ? <p className="trading-pets-error">{error}</p> : null}
    </div>
  );
};
