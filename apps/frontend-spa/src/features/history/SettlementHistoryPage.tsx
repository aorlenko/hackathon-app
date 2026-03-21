import { useEffect, useMemo, useState } from "react";
import type { SettlementRecord, TradeRecord } from "../../contracts/trading";
import { useTradingAuth } from "../auth/AuthProvider";
import {
  formatParticipantLabel,
  useResolvedAccountIdentities,
} from "../account/useResolvedAccountIdentities";
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

  const settlementsNewestFirst = useMemo(
    () =>
      [...settlements].sort((a, b) => {
        const ta = new Date(a.completedAtUtc ?? a.startedAtUtc).getTime();
        const tb = new Date(b.completedAtUtc ?? b.startedAtUtc).getTime();
        return tb - ta;
      }),
    [settlements],
  );

  useEffect(() => {
    if (!auth.userId) {
      setLoading(false);
      return;
    }

    setLoading(true);

    void Promise.all([
      getUserSettlementHistory(auth.userId, auth.accessToken).catch(
        () => [] as SettlementRecord[],
      ),
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

  const labelOpts = {
    currentUserId: auth.userId,
    currentUserEmail: auth.accountSnapshot?.email,
    currentUserDisplayName: auth.accountSnapshot?.displayName ?? auth.displayName,
  };

  return (
    <div className="trading-pets-page">
      <header className="trading-pets-page__header">
        <h1>Settlement history</h1>
        <p className="muted">
          Each row is one settlement, with trade execution details when the trade record is available.
        </p>
      </header>
      {loading ? <p className="trading-pets-loading">Loading settlements...</p> : null}
      {error ? <p className="error-text">{error}</p> : null}
      {!loading && !error && settlements.length === 0 ? (
        <p className="muted">No settlement history found for this user.</p>
      ) : null}
      {settlements.length > 0 ? (
        <div className="trading-pets-table-scroll">
          <table className="trading-pets-table trading-pets-table--settlement-history">
          <thead>
            <tr>
              <th>Trade executed</th>
              <th>Settlement started</th>
              <th>Symbol</th>
              <th>Price</th>
              <th>Qty</th>
              <th>Buyer</th>
              <th>Seller</th>
              <th>Status</th>
              <th>Completed</th>
            </tr>
          </thead>
          <tbody>
            {settlementsNewestFirst.map((settlement) => {
              const trade = tradeById.get(settlement.tradeId);

              return (
                <tr key={settlement.settlementId}>
                  <td>
                    {trade ? new Date(trade.executedAtUtc).toLocaleString() : "—"}
                  </td>
                  <td>{new Date(settlement.startedAtUtc).toLocaleString()}</td>
                  <td>{trade?.symbol ?? "—"}</td>
                  <td>{trade != null ? `$${trade.price.toFixed(2)}` : "—"}</td>
                  <td>{trade?.quantity ?? "—"}</td>
                  <td>
                    {trade
                      ? formatParticipantLabel(trade.buyerUserId, participantIdentities, {
                          ...labelOpts,
                          fallbackLabel: "Buyer",
                        })
                      : "—"}
                  </td>
                  <td>
                    {trade
                      ? formatParticipantLabel(trade.sellerUserId, participantIdentities, {
                          ...labelOpts,
                          fallbackLabel: "Seller",
                        })
                      : "—"}
                  </td>
                  <td>
                    {settlement.status}
                    {settlement.status === "FAILED" && settlement.failureReason ? (
                      <span className="muted small">
                        <br />
                        {settlement.failureReason}
                      </span>
                    ) : null}
                  </td>
                  <td>
                    {settlement.completedAtUtc
                      ? new Date(settlement.completedAtUtc).toLocaleString()
                      : "—"}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        </div>
      ) : null}
    </div>
  );
};
