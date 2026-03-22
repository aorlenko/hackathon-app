import { useEffect, useMemo, useState } from "react";
import type { SettlementRecord, TradeRecord } from "../../contracts/trading";
import { useTradingAuth } from "../auth/AuthProvider";
import {
  formatParticipantLabel,
  useResolvedAccountIdentities,
} from "../account/useResolvedAccountIdentities";
import { getUserSettlementHistory } from "./settlementHistoryApi";
import { getUserTradeHistory } from "./tradeHistoryApi";

const getTradePerspective = (
  trade: TradeRecord,
  currentUserId: string | null | undefined,
) => {
  const normalizedCurrentUserId =
    typeof currentUserId === "string" ? currentUserId.trim() : "";

  if (normalizedCurrentUserId && trade.buyerUserId === normalizedCurrentUserId) {
    return {
      sideLabel: "Buy",
      sideTone: "ok",
      counterpartyUserId: trade.sellerUserId,
      counterpartyFallbackLabel: "Seller",
    } as const;
  }

  if (normalizedCurrentUserId && trade.sellerUserId === normalizedCurrentUserId) {
    return {
      sideLabel: "Sell",
      sideTone: "warn",
      counterpartyUserId: trade.buyerUserId,
      counterpartyFallbackLabel: "Buyer",
    } as const;
  }

  return null;
};

export const TradeHistoryPage = () => {
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
      .then(([settlementHistory, tradeHistory]) => {
        setSettlements(settlementHistory);
        setTrades(tradeHistory);
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

  const labelOpts = {
    currentUserId: auth.userId,
    currentUserEmail: auth.accountSnapshot?.email,
    currentUserDisplayName: auth.accountSnapshot?.displayName ?? auth.displayName,
  };

  return (
    <div className="trading-pets-page">
      <header className="trading-pets-page__header">
        <h1>Trade history</h1>
        <p className="muted">
          Completed and in-flight trade settlements for your account, with execution details when the trade record is
          available.
        </p>
      </header>
      {loading ? <p className="trading-pets-loading">Loading trade history...</p> : null}
      {error ? <p className="error-text">{error}</p> : null}
      {!loading && !error && settlements.length === 0 ? (
        <p className="muted">No trade history found for this user.</p>
      ) : null}
      {settlements.length > 0 ? (
        <div className="trading-pets-table-scroll">
          <table className="trading-pets-table trading-pets-table--settlement-history">
            <thead>
              <tr>
                <th>Side</th>
                <th>Price</th>
                <th>Qty</th>
                <th>Symbol</th>
                <th>With</th>
                <th>Trade time</th>
                <th>Settlement time</th>
              </tr>
            </thead>
            <tbody>
              {settlementsNewestFirst.map((settlement) => {
                const trade = tradeById.get(settlement.tradeId);
                const tradePerspective = trade
                  ? getTradePerspective(trade, auth.userId)
                  : null;

                return (
                  <tr key={settlement.settlementId}>
                    <td>
                      {tradePerspective ? (
                        <span className={`tag ${tradePerspective.sideTone}`}>
                          {tradePerspective.sideLabel}
                        </span>
                      ) : (
                        "—"
                      )}
                    </td>
                    <td>{trade != null ? `$${trade.price.toFixed(2)}` : "—"}</td>
                    <td>{trade?.quantity ?? "—"}</td>
                    <td>{trade?.symbol ?? "—"}</td>
                    <td>
                      {trade && tradePerspective
                        ? formatParticipantLabel(
                            tradePerspective.counterpartyUserId,
                            participantIdentities,
                            {
                              ...labelOpts,
                              fallbackLabel:
                                tradePerspective.counterpartyFallbackLabel,
                            },
                          )
                        : "—"}
                    </td>
                    <td>{trade ? new Date(trade.executedAtUtc).toLocaleString() : "—"}</td>
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
