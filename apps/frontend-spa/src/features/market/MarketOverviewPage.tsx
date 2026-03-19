import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import type { MarketSummary } from "../../contracts/trading";
import { getMarkets } from "./marketApi";

export const MarketOverviewPage = () => {
  const [markets, setMarkets] = useState<MarketSummary[]>([]);
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    void getMarkets()
      .then((response) => {
        setMarkets(response);
        setError("");
      })
      .catch((requestError: unknown) => {
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Markets could not be loaded.",
        );
      })
      .finally(() => setLoading(false));
  }, []);

  return (
    <section className="stack">
      <section className="card">
        <h2>Market catalog</h2>
        <p className="muted">
          Select a market to place orders and follow live lifecycle updates.
        </p>
      </section>
      <section className="card">
        {loading ? <p>Loading markets...</p> : null}
        {error ? <p className="error-text">{error}</p> : null}
        {!loading && !error && markets.length === 0 ? (
          <p className="muted">No tradable items are currently available.</p>
        ) : null}
        <div className="market-grid">
          {markets.map((market) => (
            <article key={market.symbol} className="market-card">
              <div className="section-header">
                <div>
                  <h3>{market.symbol}</h3>
                  <p className="muted">
                    {market.name} · {market.category}
                  </p>
                </div>
                <span className={`tag ${market.isTradable ? "ok" : "warn"}`}>
                  {market.isTradable ? "Tradable" : "Paused"}
                </span>
              </div>
              <p>Reference price: {market.referencePrice.toFixed(2)}</p>
              <Link className="primary-button inline-link" to={`/markets/${market.symbol}`}>
                Open market
              </Link>
            </article>
          ))}
        </div>
      </section>
    </section>
  );
};
