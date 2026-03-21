import { useEffect, useMemo, useState } from "react";
import { useTradingAuth } from "../auth/AuthProvider";
import type { TradeRecord } from "../../contracts/trading";
import {
  formatParticipantLabel,
  useResolvedAccountIdentities,
} from "../account/useResolvedAccountIdentities";
import { getUserTradeHistory } from "./tradeHistoryApi";

export const TradeHistoryPage = () => {
  const auth = useTradingAuth();
  const [trades, setTrades] = useState<TradeRecord[]>([]);
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(true);
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
    <div className="trading-pets-page">
      <header className="trading-pets-page__header">
        <h1>Trade history</h1>
        <p className="muted">Trades recorded for your account.</p>
      </header>
      {loading ? <p className="trading-pets-loading">Loading trades...</p> : null}
      {error ? <p className="error-text">{error}</p> : null}
      {!loading && !error && trades.length === 0 ? (
        <p className="muted">No historical trades found for this user.</p>
      ) : null}
      {trades.length > 0 ? (
        <table className="trading-pets-table">
            <thead>
              <tr>
                <th>Executed</th>
                <th>Symbol</th>
                <th>Price</th>
                <th>Quantity</th>
                <th>Buyer</th>
                <th>Seller</th>
              </tr>
            </thead>
            <tbody>
              {trades.map((trade) => (
                <tr key={trade.tradeId}>
                  <td>{new Date(trade.executedAtUtc).toLocaleString()}</td>
                  <td>{trade.symbol}</td>
                  <td>{trade.price.toFixed(2)}</td>
                  <td>{trade.quantity}</td>
                  <td>
                    {formatParticipantLabel(
                      trade.buyerUserId,
                      participantIdentities,
                      {
                        currentUserId: auth.userId,
                        currentUserEmail: auth.accountSnapshot?.email,
                        currentUserDisplayName:
                          auth.accountSnapshot?.displayName ?? auth.displayName,
                        fallbackLabel: "Buyer",
                      },
                    )}
                  </td>
                  <td>
                    {formatParticipantLabel(
                      trade.sellerUserId,
                      participantIdentities,
                      {
                        currentUserId: auth.userId,
                        currentUserEmail: auth.accountSnapshot?.email,
                        currentUserDisplayName:
                          auth.accountSnapshot?.displayName ?? auth.displayName,
                        fallbackLabel: "Seller",
                      },
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
        </table>
      ) : null}
    </div>
  );
};
