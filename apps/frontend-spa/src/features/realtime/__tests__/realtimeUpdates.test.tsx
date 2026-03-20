import { act, render, screen, waitFor } from "@testing-library/react";
import { useEffect, useState } from "react";
import type {
  FundsUpdatedMessage,
  OrderBookUpdatedMessage,
  SettlementUpdatedMessage,
  TradeRecordedMessage,
} from "../../../contracts/trading";
import { useAccountFunds } from "../../account/useAccountFunds";
import type { MarketHubClient } from "../marketHubClient";
import { useRealtimeLifecycle } from "../useRealtimeLifecycle";

const EMPTY_TRADES: Array<{
  tradeId: string;
  symbol: string;
  price: number;
  quantity: number;
  executedAtUtc: string;
  buyerUserId: string;
  sellerUserId: string;
}> = [];

const EMPTY_SETTLEMENTS: Array<{
  settlementId: string;
  tradeId: string;
  status: "PENDING" | "IN_PROGRESS" | "SETTLED" | "FAILED";
  startedAtUtc: string;
  completedAtUtc: string | null;
  failureReason: string | null;
}> = [];

class FakeMarketHubClient implements MarketHubClient {
  private orderBookHandlers = new Set<(message: OrderBookUpdatedMessage) => void>();
  private tradeHandlers = new Set<(message: TradeRecordedMessage) => void>();
  private fundsHandlers = new Set<(message: FundsUpdatedMessage) => void>();
  private settlementHandlers = new Set<
    (message: SettlementUpdatedMessage) => void
  >();
  private reconnectHandlers = new Set<() => void>();

  async start() {
    return undefined;
  }

  async stop() {
    return undefined;
  }

  async joinMarket(_symbol: string) {
    return undefined;
  }

  async leaveMarket(_symbol: string) {
    return undefined;
  }

  onOrderBookUpdated(handler: (message: OrderBookUpdatedMessage) => void) {
    this.orderBookHandlers.add(handler);
    return () => this.orderBookHandlers.delete(handler);
  }

  onTradeRecorded(handler: (message: TradeRecordedMessage) => void) {
    this.tradeHandlers.add(handler);
    return () => this.tradeHandlers.delete(handler);
  }

  onFundsUpdated(handler: (message: FundsUpdatedMessage) => void) {
    this.fundsHandlers.add(handler);
    return () => this.fundsHandlers.delete(handler);
  }

  onSettlementUpdated(handler: (message: SettlementUpdatedMessage) => void) {
    this.settlementHandlers.add(handler);
    return () => this.settlementHandlers.delete(handler);
  }

  onReconnected(handler: () => void) {
    this.reconnectHandlers.add(handler);
    return () => this.reconnectHandlers.delete(handler);
  }

  emitOrderBook(message: OrderBookUpdatedMessage) {
    this.orderBookHandlers.forEach((handler) => handler(message));
  }

  emitTrade(message: TradeRecordedMessage) {
    this.tradeHandlers.forEach((handler) => handler(message));
  }

  emitFunds(message: FundsUpdatedMessage) {
    this.fundsHandlers.forEach((handler) => handler(message));
  }

  emitSettlement(message: SettlementUpdatedMessage) {
    this.settlementHandlers.forEach((handler) => handler(message));
  }

  emitReconnect() {
    this.reconnectHandlers.forEach((handler) => handler());
  }
}

const TestHarness = ({
  client,
  initialOrderBook = null,
  initialTrades = EMPTY_TRADES,
  initialSettlements = EMPTY_SETTLEMENTS,
  refreshSnapshots,
}: {
  client: MarketHubClient;
  initialOrderBook?: {
    symbol: string;
    bids: Array<{ price: number; quantity: number; orderCount: number }>;
    asks: Array<{ price: number; quantity: number; orderCount: number }>;
    lastUpdatedAtUtc: string;
  } | null;
  initialTrades?: Array<{
    tradeId: string;
    symbol: string;
    price: number;
    quantity: number;
    executedAtUtc: string;
    buyerUserId: string;
    sellerUserId: string;
  }>;
  initialSettlements?: Array<{
    settlementId: string;
    tradeId: string;
    status: "PENDING" | "IN_PROGRESS" | "SETTLED" | "FAILED";
    startedAtUtc: string;
    completedAtUtc: string | null;
    failureReason: string | null;
  }>;
  refreshSnapshots: () => Promise<{
    orderBook: {
      symbol: string;
      bids: Array<{ price: number; quantity: number; orderCount: number }>;
      asks: Array<{ price: number; quantity: number; orderCount: number }>;
      lastUpdatedAtUtc: string;
    } | null;
    trades: Array<{
      tradeId: string;
      symbol: string;
      price: number;
      quantity: number;
      executedAtUtc: string;
      buyerUserId: string;
      sellerUserId: string;
    }>;
    settlements: Array<{
      settlementId: string;
      tradeId: string;
      status: "PENDING" | "IN_PROGRESS" | "SETTLED" | "FAILED";
      startedAtUtc: string;
      completedAtUtc: string | null;
      failureReason: string | null;
    }>;
  }>;
}) => {
  const realtime = useRealtimeLifecycle({
    symbol: "ABC",
    initialOrderBook,
    initialTrades,
    initialSettlements,
    client,
    refreshSnapshots,
  });

  useEffect(() => {
    document.body.dataset.state = realtime.connectionState;
  }, [realtime.connectionState]);

  return (
    <div>
      <div data-testid="orderbook-quantity">
        {realtime.orderBook?.bids[0]?.quantity ?? 0}
      </div>
      <div data-testid="trades-count">{realtime.trades.length}</div>
      <div data-testid="settlements-count">{realtime.settlements.length}</div>
    </div>
  );
};

const FundsHarness = ({ client }: { client: MarketHubClient }) => {
  const funds = useAccountFunds({
    accessToken: "token",
    client,
    initialSnapshot: {
      userId: "user-1",
      displayName: "Buyer One",
      email: "user1@example.com",
      cashAvailable: 250000,
      holdings: [{ symbol: "ABC", quantity: 10 }],
    },
    isAuthenticated: true,
    userId: "user-1",
  });

  return (
    <div>
      <div data-testid="funds-amount">{funds.formattedFunds}</div>
      <div data-testid="funds-state">{funds.state}</div>
    </div>
  );
};

const PropSyncHarness = ({ client }: { client: MarketHubClient }) => {
  const [orderBook, setOrderBook] = useState<{
    symbol: string;
    bids: Array<{ price: number; quantity: number; orderCount: number }>;
    asks: Array<{ price: number; quantity: number; orderCount: number }>;
    lastUpdatedAtUtc: string;
  } | null>(null);
  const [trades, setTrades] = useState<
    Array<{
      tradeId: string;
      symbol: string;
      price: number;
      quantity: number;
      executedAtUtc: string;
      buyerUserId: string;
      sellerUserId: string;
    }>
  >([]);
  const [settlements, setSettlements] = useState<
    Array<{
      settlementId: string;
      tradeId: string;
      status: "PENDING" | "IN_PROGRESS" | "SETTLED" | "FAILED";
      startedAtUtc: string;
      completedAtUtc: string | null;
      failureReason: string | null;
    }>
  >([]);

  return (
    <div>
      <button
        onClick={() => {
          setOrderBook({
            symbol: "ABC",
            bids: [{ price: 104, quantity: 7, orderCount: 1 }],
            asks: [],
            lastUpdatedAtUtc: "2026-03-17T12:00:00Z",
          });
          setTrades([
            {
              tradeId: "trade-prop-sync",
              symbol: "ABC",
              price: 104,
              quantity: 2,
              executedAtUtc: "2026-03-17T12:00:01Z",
              buyerUserId: "buyer",
              sellerUserId: "seller",
            },
          ]);
          setSettlements([
            {
              settlementId: "settlement-prop-sync",
              tradeId: "trade-prop-sync",
              status: "PENDING",
              startedAtUtc: "2026-03-17T12:00:02Z",
              completedAtUtc: null,
              failureReason: null,
            },
          ]);
        }}
      >
        Refresh snapshots
      </button>
      <TestHarness
        client={client}
        initialOrderBook={orderBook}
        initialTrades={trades}
        initialSettlements={settlements}
        refreshSnapshots={async () => ({
          orderBook,
          trades,
          settlements,
        })}
      />
    </div>
  );
};

describe("useRealtimeLifecycle", () => {
  it("syncs when parent snapshots change after initial render", async () => {
    const client = new FakeMarketHubClient();

    render(<PropSyncHarness client={client} />);

    await waitFor(() =>
      expect(document.body.dataset.state).toBe("connected"),
    );

    expect(screen.getByTestId("orderbook-quantity")).toHaveTextContent("0");
    expect(screen.getByTestId("trades-count")).toHaveTextContent("0");
    expect(screen.getByTestId("settlements-count")).toHaveTextContent("0");

    await act(async () => {
      screen.getByRole("button", { name: "Refresh snapshots" }).click();
    });

    await waitFor(() =>
      expect(screen.getByTestId("orderbook-quantity")).toHaveTextContent("7"),
    );
    expect(screen.getByTestId("trades-count")).toHaveTextContent("1");
    expect(screen.getByTestId("settlements-count")).toHaveTextContent("1");
  });

  it("applies hub updates and refreshes snapshots after reconnect", async () => {
    const client = new FakeMarketHubClient();
    const refreshSnapshots = vi.fn().mockResolvedValue({
      orderBook: {
        symbol: "ABC",
        bids: [{ price: 102, quantity: 9, orderCount: 1 }],
        asks: [],
        lastUpdatedAtUtc: "2026-03-13T10:00:00Z",
      },
      trades: [
        {
          tradeId: "trade-2",
          symbol: "ABC",
          price: 102,
          quantity: 1,
          executedAtUtc: "2026-03-13T10:00:00Z",
          buyerUserId: "buyer",
          sellerUserId: "seller",
        },
      ],
      settlements: [
        {
          settlementId: "settlement-2",
          tradeId: "trade-2",
          status: "SETTLED",
          startedAtUtc: "2026-03-13T10:00:01Z",
          completedAtUtc: "2026-03-13T10:00:04Z",
          failureReason: null,
        },
      ],
    });

    render(<TestHarness client={client} refreshSnapshots={refreshSnapshots} />);

    await waitFor(() =>
      expect(document.body.dataset.state).toBe("connected"),
    );

    await act(async () => {
      client.emitOrderBook({
        version: 1,
        payload: {
          symbol: "ABC",
          bids: [{ price: 101, quantity: 3, orderCount: 1 }],
          asks: [],
          lastUpdatedAtUtc: "2026-03-13T10:00:00Z",
        },
      });
      client.emitTrade({
        version: 1,
        payload: {
          tradeId: "trade-1",
          symbol: "ABC",
          price: 101,
          quantity: 3,
          executedAtUtc: "2026-03-13T10:00:00Z",
          buyerUserId: "buyer",
          sellerUserId: "seller",
        },
      });
      client.emitSettlement({
        version: 1,
        payload: {
          settlementId: "settlement-1",
          tradeId: "trade-1",
          status: "IN_PROGRESS",
          startedAtUtc: "2026-03-13T10:00:01Z",
          completedAtUtc: null,
          failureReason: null,
        },
      });
    });

    expect(screen.getByTestId("orderbook-quantity")).toHaveTextContent("3");
    expect(screen.getByTestId("trades-count")).toHaveTextContent("1");
    expect(screen.getByTestId("settlements-count")).toHaveTextContent("1");

    await act(async () => {
      client.emitReconnect();
    });

    await waitFor(() => expect(refreshSnapshots).toHaveBeenCalledTimes(1));
    await waitFor(() =>
      expect(screen.getByTestId("orderbook-quantity")).toHaveTextContent("9"),
    );
  });

  it("updates confirmed account funds from FundsUpdated realtime events", async () => {
    const client = new FakeMarketHubClient();

    render(<FundsHarness client={client} />);

    await waitFor(() =>
      expect(screen.getByTestId("funds-amount")).toHaveTextContent(
        "$250,000.00",
      ),
    );

    await act(async () => {
      client.emitFunds({
        version: 1,
        payload: {
          userId: "user-1",
          cashAvailable: 249700,
          changedAtUtc: "2026-03-19T15:30:00Z",
        },
      });
    });

    expect(screen.getByTestId("funds-amount")).toHaveTextContent("$249,700.00");
    expect(screen.getByTestId("funds-state")).toHaveTextContent("confirmed");
  });
});
