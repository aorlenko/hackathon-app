import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import type {
  OrderBookSnapshot,
  PlaceOrderRequest,
  SettlementRecord,
  TradeRecord,
} from "../../contracts/trading";
import { useTradingAuth } from "../auth/AuthProvider";
import { useResolvedAccountIdentities } from "../account/useResolvedAccountIdentities";
import { ACCOUNT_FUNDS_REFRESH_EVENT } from "../account/useAccountFunds";
import { getUserSettlementHistory } from "../history/settlementHistoryApi";
import { createMarketHubClient } from "../realtime/marketHubClient";
import { useRealtimeLifecycle } from "../realtime/useRealtimeLifecycle";
import { OrderBookPanel } from "./OrderBookPanel";
import { OrderEntryForm } from "./OrderEntryForm";
import { RecentTradesPanel } from "./RecentTradesPanel";
import { SettlementStatusPanel } from "./SettlementStatusPanel";
import { getOrderBook, getRecentTrades, placeOrder } from "./marketApi";

export const MarketDetailPage = () => {
  const { symbol = "" } = useParams();
  const auth = useTradingAuth();
  const [orderBook, setOrderBook] = useState<OrderBookSnapshot | null>(null);
  const [trades, setTrades] = useState<TradeRecord[]>([]);
  const [settlements, setSettlements] = useState<SettlementRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const loadSnapshots = useCallback(async () => {
    if (!symbol) {
      return {
        orderBook: null,
        trades: [],
        settlements: [],
      };
    }

    const [orderBookSnapshot, recentTrades, settlementHistory] = await Promise.all([
      getOrderBook(symbol, auth.accessToken),
      getRecentTrades(symbol, auth.accessToken),
      auth.userId
        ? getUserSettlementHistory(auth.userId, auth.accessToken)
        : Promise.resolve([] as SettlementRecord[]),
    ]);

    return {
      orderBook: orderBookSnapshot,
      trades: recentTrades,
      settlements: settlementHistory,
    };
  }, [auth.accessToken, auth.userId, symbol]);

  useEffect(() => {
    setLoading(true);
    void loadSnapshots()
      .then((snapshot) => {
        setOrderBook(snapshot.orderBook);
        setTrades(snapshot.trades);
        setSettlements(snapshot.settlements);
        setError("");
      })
      .catch((requestError: unknown) => {
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Market data could not be loaded.",
        );
      })
      .finally(() => setLoading(false));
  }, [loadSnapshots]);

  const realtimeClient = useMemo(
    () =>
      auth.accessToken
        ? createMarketHubClient(async () => auth.accessToken)
        : null,
    [auth.accessToken],
  );

  const realtime = useRealtimeLifecycle({
    symbol,
    initialOrderBook: orderBook,
    initialTrades: trades,
    initialSettlements: settlements,
    client: realtimeClient,
    refreshSnapshots: loadSnapshots,
  });
  const participantUserIds = useMemo(
    () =>
      realtime.trades.flatMap((trade) => [trade.buyerUserId, trade.sellerUserId]),
    [realtime.trades],
  );
  const participantIdentities = useResolvedAccountIdentities(
    participantUserIds,
    auth.accessToken,
  );

  const handleSubmitOrder = (input: Omit<PlaceOrderRequest, "itemSymbol">) =>
    placeOrder(
      {
        itemSymbol: symbol,
        ...input,
      },
      auth.accessToken,
    ).then(async (response) => {
      const snapshot = await loadSnapshots();
      setOrderBook(snapshot.orderBook);
      setTrades(snapshot.trades);
      setSettlements(snapshot.settlements);
      window.dispatchEvent(new Event(ACCOUNT_FUNDS_REFRESH_EVENT));
      return response;
    });

  return (
    <section className="stack">
      <section className="card">
        <div className="section-header">
          <div>
            <h2>Market detail</h2>
            <p className="muted">
              Track the full order to settlement lifecycle for {symbol}.
            </p>
          </div>
        </div>
        {realtime.connectionState !== "connected" ? (
          <p className="muted small inline-feedback">
            Live updates are currently unavailable. Data will continue refreshing
            after order actions and page reloads.
          </p>
        ) : null}
      </section>

      {loading ? <section className="card">Loading market...</section> : null}
      {error ? <section className="card error-text">{error}</section> : null}

      {!loading && !error ? (
        <>
          <div className="dashboard-grid">
            <OrderEntryForm
              symbol={symbol}
              disabled={!auth.isAuthenticated}
              onSubmit={handleSubmitOrder}
            />
            <OrderBookPanel
              orderBook={realtime.orderBook}
              connectionState={realtime.connectionState}
            />
          </div>
          <div className="dashboard-grid">
            <RecentTradesPanel
              trades={realtime.trades}
              identities={participantIdentities}
              currentUserId={auth.userId}
              currentUserEmail={auth.accountSnapshot?.email}
              currentUserDisplayName={
                auth.accountSnapshot?.displayName ?? auth.displayName
              }
            />
            <SettlementStatusPanel settlements={realtime.settlements} />
          </div>
        </>
      ) : null}
    </section>
  );
};
