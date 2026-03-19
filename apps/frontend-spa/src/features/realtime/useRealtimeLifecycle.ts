import { useEffect, useMemo, useState } from "react";
import type {
  OrderBookSnapshot,
  SettlementRecord,
  TradeRecord,
} from "../../contracts/trading";
import type { MarketHubClient } from "./marketHubClient";

const MAX_ITEMS = 20;

const upsertTrade = (items: TradeRecord[], trade: TradeRecord) =>
  [trade, ...items.filter((item) => item.tradeId !== trade.tradeId)].slice(
    0,
    MAX_ITEMS,
  );

const upsertSettlement = (
  items: SettlementRecord[],
  settlement: SettlementRecord,
) => {
  const existing = items.find(
    (item) =>
      item.settlementId === settlement.settlementId ||
      item.tradeId === settlement.tradeId,
  );

  const nextSettlement: SettlementRecord = {
    ...settlement,
    settlementId: existing?.settlementId ?? settlement.settlementId,
    startedAtUtc: existing?.startedAtUtc ?? settlement.startedAtUtc,
    completedAtUtc: settlement.completedAtUtc ?? existing?.completedAtUtc ?? null,
    failureReason: settlement.failureReason ?? existing?.failureReason ?? null,
  };

  return [
    nextSettlement,
    ...items.filter(
      (item) =>
        item.settlementId !== nextSettlement.settlementId &&
        item.tradeId !== nextSettlement.tradeId,
    ),
  ]
    .sort(
      (left, right) =>
        new Date(right.startedAtUtc).getTime() -
        new Date(left.startedAtUtc).getTime(),
    )
    .slice(0, MAX_ITEMS);
};

export interface RealtimeLifecycleOptions {
  symbol: string;
  initialOrderBook: OrderBookSnapshot | null;
  initialTrades: TradeRecord[];
  initialSettlements: SettlementRecord[];
  client: MarketHubClient;
  refreshSnapshots?: () => Promise<{
    orderBook: OrderBookSnapshot | null;
    trades: TradeRecord[];
    settlements: SettlementRecord[];
  }>;
}

export const useRealtimeLifecycle = ({
  symbol,
  initialOrderBook,
  initialTrades,
  initialSettlements,
  client,
  refreshSnapshots,
}: RealtimeLifecycleOptions) => {
  const [orderBook, setOrderBook] = useState<OrderBookSnapshot | null>(
    initialOrderBook,
  );
  const [trades, setTrades] = useState<TradeRecord[]>(initialTrades);
  const [settlements, setSettlements] =
    useState<SettlementRecord[]>(initialSettlements);
  const [connectionState, setConnectionState] = useState<
    "connecting" | "connected" | "reconnecting" | "offline"
  >("connecting");

  useEffect(() => {
    setOrderBook(initialOrderBook);
  }, [initialOrderBook]);

  useEffect(() => {
    setTrades(initialTrades);
  }, [initialTrades]);

  useEffect(() => {
    setSettlements(initialSettlements);
  }, [initialSettlements]);

  useEffect(() => {
    let active = true;

    const orderBookUnsubscribe = client.onOrderBookUpdated((message) => {
      if (active) {
        setOrderBook(message.payload);
      }
    });
    const tradeUnsubscribe = client.onTradeRecorded((message) => {
      if (active && message.payload.symbol === symbol) {
        setTrades((current) => upsertTrade(current, message.payload));
      }
    });
    const settlementUnsubscribe = client.onSettlementUpdated((message) => {
      if (active) {
        setSettlements((current) => upsertSettlement(current, message.payload));
      }
    });
    const reconnectUnsubscribe = client.onReconnected(async () => {
      if (!active) {
        return;
      }

      setConnectionState("reconnecting");

      if (!refreshSnapshots) {
        setConnectionState("connected");
        return;
      }

      const snapshot = await refreshSnapshots();
      if (!active) {
        return;
      }

      setOrderBook(snapshot.orderBook);
      setTrades(snapshot.trades);
      setSettlements(snapshot.settlements);
      setConnectionState("connected");
    });

    const connect = async () => {
      try {
        setConnectionState("connecting");
        await client.start();
        await client.joinMarket(symbol);
        if (active) {
          setConnectionState("connected");
        }
      } catch {
        if (active) {
          setConnectionState("offline");
        }
      }
    };

    void connect();

    return () => {
      active = false;
      orderBookUnsubscribe();
      tradeUnsubscribe();
      settlementUnsubscribe();
      reconnectUnsubscribe();
      void client.leaveMarket(symbol).finally(() => client.stop());
    };
  }, [client, refreshSnapshots, symbol]);

  return useMemo(
    () => ({
      orderBook,
      trades,
      settlements,
      connectionState,
    }),
    [connectionState, orderBook, settlements, trades],
  );
};
