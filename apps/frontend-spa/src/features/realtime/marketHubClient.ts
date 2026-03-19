import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { env } from "../../config/env";
import type {
  OrderBookSnapshot,
  OrderBookUpdatedMessage,
  RealtimeEnvelope,
  SettlementRecord,
  SettlementUpdatedMessage,
  TradeRecord,
  TradeRecordedMessage,
} from "../../contracts/trading";

export interface MarketHubClient {
  start(): Promise<void>;
  stop(): Promise<void>;
  joinMarket(symbol: string): Promise<void>;
  leaveMarket(symbol: string): Promise<void>;
  onOrderBookUpdated(handler: (message: OrderBookUpdatedMessage) => void): () => void;
  onTradeRecorded(handler: (message: TradeRecordedMessage) => void): () => void;
  onSettlementUpdated(
    handler: (message: SettlementUpdatedMessage) => void,
  ): () => void;
  onReconnected(handler: () => void): () => void;
}

type RawTradeRecordedMessage = TradeRecord & { version?: number };

type RawSettlementUpdatedMessage = {
  tradeId: string;
  status: SettlementRecord["status"];
  changedAtUtc: string;
  failureReason: string | null;
  version?: number;
};

const isEnvelope = <TPayload,>(
  message: unknown,
): message is RealtimeEnvelope<TPayload> =>
  typeof message === "object" &&
  message !== null &&
  "payload" in message &&
  "version" in message;

const normalizeOrderBookMessage = (
  message: OrderBookUpdatedMessage | OrderBookSnapshot,
): OrderBookUpdatedMessage =>
  isEnvelope<OrderBookSnapshot>(message)
    ? message
    : {
        version: 1,
        payload: message,
      };

const normalizeTradeMessage = (
  message: TradeRecordedMessage | RawTradeRecordedMessage,
): TradeRecordedMessage => {
  if (isEnvelope<TradeRecord>(message)) {
    return message;
  }

  const { version = 1, ...payload } = message;
  return {
    version,
    payload,
  };
};

const normalizeSettlementMessage = (
  message: SettlementUpdatedMessage | RawSettlementUpdatedMessage,
): SettlementUpdatedMessage => {
  if (isEnvelope<SettlementRecord>(message)) {
    return message;
  }

  const { version = 1, tradeId, status, changedAtUtc, failureReason } = message;
  return {
    version,
    payload: {
      settlementId: tradeId,
      tradeId,
      status,
      startedAtUtc: changedAtUtc,
      completedAtUtc:
        status === "SETTLED" || status === "FAILED" ? changedAtUtc : null,
      failureReason,
    },
  };
};

const registerHandler = <TRawMessage, TMessage>(
  connection: HubConnection,
  eventName: string,
  normalize: (message: TRawMessage) => TMessage,
  handler: (message: TMessage) => void,
) => {
  const normalizedHandler = (message: TRawMessage) => handler(normalize(message));
  connection.on(eventName, normalizedHandler);
  return () => connection.off(eventName, normalizedHandler);
};

export const createMarketHubClient = (
  accessTokenFactory?: () => Promise<string | undefined>,
): MarketHubClient => {
  const reconnectHandlers = new Set<() => void>();
  const builder = new HubConnectionBuilder()
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning);

  const connection = (accessTokenFactory
    ? builder.withUrl(env.marketHubUrl, {
        accessTokenFactory: async () => (await accessTokenFactory()) ?? "",
      })
    : builder.withUrl(env.marketHubUrl)
  ).build();

  connection.onreconnected(() => {
    reconnectHandlers.forEach((handler) => handler());
  });

  return {
    async start() {
      if (connection.state === HubConnectionState.Disconnected) {
        await connection.start();
      }
    },
    async stop() {
      if (connection.state !== HubConnectionState.Disconnected) {
        await connection.stop();
      }
    },
    async joinMarket(symbol: string) {
      await connection.invoke("JoinMarket", symbol);
    },
    async leaveMarket(symbol: string) {
      if (connection.state === HubConnectionState.Connected) {
        await connection.invoke("LeaveMarket", symbol);
      }
    },
    onOrderBookUpdated(handler) {
      return registerHandler(
        connection,
        "OrderBookUpdated",
        normalizeOrderBookMessage,
        handler,
      );
    },
    onTradeRecorded(handler) {
      return registerHandler(
        connection,
        "TradeRecorded",
        normalizeTradeMessage,
        handler,
      );
    },
    onSettlementUpdated(handler) {
      return registerHandler(
        connection,
        "SettlementUpdated",
        normalizeSettlementMessage,
        handler,
      );
    },
    onReconnected(handler) {
      reconnectHandlers.add(handler);
      return () => reconnectHandlers.delete(handler);
    },
  };
};
