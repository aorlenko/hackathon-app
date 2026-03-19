import { useEffect, useState } from "react";
import { useTradingAuth } from "../auth/AuthProvider";
import type { TradeRecord } from "../../contracts/trading";
import { getUserTradeHistory } from "./tradeHistoryApi";

export const TradeHistoryPage = () => {
  const auth = useTradingAuth();
  const [trades, setTrades] = useState<TradeRecord[]>([]);
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!auth.userId) {
      setLoading(false);
      return;
    }

    setLoading(true);
    void getUserTradeHistory(auth.userId, auth.accessToken)
      .then((response) => {
        setTrades(response);
        setError("");
      })
      .catch((requestError: unknown) => {
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Trade history could not be loaded.",
        );
      })
      .finally(() => setLoading(false));
  }, [auth.accessToken, auth.userId]);

  return (
    <section className="stack">
      <section className="card">
        <h2>Trade history</h2>
        <p className="muted">
          Review recent executions returned by the trade service.
        </p>
      </section>
      <section className="card">
        {loading ? <p>Loading trades...</p> : null}
        {error ? <p className="error-text">{error}</p> : null}
        {!loading && !error && trades.length === 0 ? (
          <p className="muted">No historical trades found for this user.</p>
        ) : null}
        {trades.length > 0 ? (
          <table className="data-table">
            <thead>
              <tr>
                <th>Trade</th>
                <th>Symbol</th>
                <th>Price</th>
                <th>Quantity</th>
                <th>Executed</th>
              </tr>
            </thead>
            <tbody>
              {trades.map((trade) => (
                <tr key={trade.tradeId}>
                  <td>{trade.tradeId}</td>
                  <td>{trade.symbol}</td>
                  <td>{trade.price.toFixed(2)}</td>
                  <td>{trade.quantity}</td>
                  <td>{new Date(trade.executedAtUtc).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </section>
    </section>
  );
};
