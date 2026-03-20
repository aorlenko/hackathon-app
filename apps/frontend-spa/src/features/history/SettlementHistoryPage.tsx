import { useEffect, useMemo, useState } from "react";
import type { SettlementRecord, TradeRecord } from "../../contracts/trading";
import { useTradingAuth } from "../auth/AuthProvider";
import {
  formatParticipantLabel,
  useResolvedAccountIdentities,
} from "../account/useResolvedAccountIdentities";
import { LifecycleTraceTable } from "./LifecycleTraceTable";
import { getUserSettlementHistory } from "./settlementHistoryApi";
import { getUserTradeHistory } from "./tradeHistoryApi";

export const SettlementHistoryPage = () => {
  const auth = useTradingAuth();
  const [settlements, setSettlements] = useState<SettlementRecord[]>([]);
  const [trades, setTrades] = useState<TradeRecord[]>([]);
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const tradeById = useMemo(
    () => new Map(trades.map((trade) => [trade.tradeId, trade])),
    [trades],
  );
  const participantUserIds = useMemo(
    () => trades.flatMap((trade) => [trade.buyerUserId, trade.sellerUserId]),
    [trades],
  );
  const participantIdentities = useResolvedAccountIdentities(
    participantUserIds,
    auth.accessToken,
  );

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
                <th>Started</th>
                <th>Symbol</th>
                <th>Buyer</th>
                <th>Seller</th>
                <th>Status</th>
                <th>Completed</th>
              </tr>
            </thead>
            <tbody>
              {settlements.map((settlement) => {
                const trade = tradeById.get(settlement.tradeId);

                return (
                  <tr key={settlement.settlementId}>
                    <td>{new Date(settlement.startedAtUtc).toLocaleString()}</td>
                    <td>{trade?.symbol ?? "-"}</td>
                    <td>
                      {trade
                        ? formatParticipantLabel(
                            trade.buyerUserId,
                            participantIdentities,
                            {
                              currentUserId: auth.userId,
                              currentUserEmail: auth.accountSnapshot?.email,
                              fallbackLabel: "Buyer",
                            },
                          )
                        : "-"}
                    </td>
                    <td>
                      {trade
                        ? formatParticipantLabel(
                            trade.sellerUserId,
                            participantIdentities,
                            {
                              currentUserId: auth.userId,
                              currentUserEmail: auth.accountSnapshot?.email,
                              fallbackLabel: "Seller",
                            },
                          )
                        : "-"}
                    </td>
                    <td>{settlement.status}</td>
                    <td>
                      {settlement.completedAtUtc
                        ? new Date(settlement.completedAtUtc).toLocaleString()
                        : "-"}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        ) : null}
      </section>
      <LifecycleTraceTable
        trades={trades}
        settlements={settlements}
        identities={participantIdentities}
        currentUserId={auth.userId}
        currentUserEmail={auth.accountSnapshot?.email}
      />
    </section>
  );
};
