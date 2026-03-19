import { useEffect, useState } from "react";
import type { SettlementRecord, TradeRecord } from "../../contracts/trading";
import { useTradingAuth } from "../auth/AuthProvider";
import { LifecycleTraceTable } from "./LifecycleTraceTable";
import { getUserSettlementHistory } from "./settlementHistoryApi";
import { getUserTradeHistory } from "./tradeHistoryApi";

export const SettlementHistoryPage = () => {
  const auth = useTradingAuth();
  const [settlements, setSettlements] = useState<SettlementRecord[]>([]);
  const [trades, setTrades] = useState<TradeRecord[]>([]);
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!auth.userId) {
      setLoading(false);
      return;
    }

    setLoading(true);

    void Promise.all([
      getUserSettlementHistory(auth.userId, auth.accessToken),
      getUserTradeHistory(auth.userId, auth.accessToken).catch(
        () => [] as TradeRecord[],
      ),
    ])
      .then(([settlementHistory, recentTrades]) => {
        setSettlements(settlementHistory);
        setTrades(recentTrades);
        setError("");
      })
      .catch((requestError: unknown) => {
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Settlement history could not be loaded.",
        );
      })
      .finally(() => setLoading(false));
  }, [auth.accessToken, auth.userId]);

  return (
    <section className="stack">
      <section className="card">
        <h2>Settlement history</h2>
        <p className="muted">
          Monitor terminal settlement outcomes and lifecycle completion context.
        </p>
      </section>
      <section className="card">
        {loading ? <p>Loading settlements...</p> : null}
        {error ? <p className="error-text">{error}</p> : null}
        {!loading && !error && settlements.length === 0 ? (
          <p className="muted">No settlement history found for this user.</p>
        ) : null}
        {settlements.length > 0 ? (
          <table className="data-table">
            <thead>
              <tr>
                <th>Settlement</th>
                <th>Trade</th>
                <th>Status</th>
                <th>Started</th>
                <th>Completed</th>
              </tr>
            </thead>
            <tbody>
              {settlements.map((settlement) => (
                <tr key={settlement.settlementId}>
                  <td>{settlement.settlementId}</td>
                  <td>{settlement.tradeId}</td>
                  <td>{settlement.status}</td>
                  <td>{new Date(settlement.startedAtUtc).toLocaleString()}</td>
                  <td>
                    {settlement.completedAtUtc
                      ? new Date(settlement.completedAtUtc).toLocaleString()
                      : "-"}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : null}
      </section>
      <LifecycleTraceTable trades={trades} settlements={settlements} />
    </section>
  );
};
