export type OrderSide = "BUY" | "SELL";
export type OrderStatus = "OPEN" | "PARTIALLY_FILLED" | "FILLED";
export type SettlementStatus = "PENDING" | "IN_PROGRESS" | "SETTLED" | "FAILED";

export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, unknown>;
  correlationId?: string;
}

export interface MarketSummary {
  symbol: string;
  name: string;
  category: string;
  referencePrice: number;
  isTradable: boolean;
}

export interface OrderBookLevel {
  price: number;
  quantity: number;
  orderCount: number;
}

export interface OrderBookSnapshot {
  symbol: string;
  bids: OrderBookLevel[];
  asks: OrderBookLevel[];
  lastUpdatedAtUtc: string;
}

export interface PlaceOrderRequest {
  itemSymbol: string;
  side: OrderSide;
  price: number;
  quantity: number;
}

export interface PlaceOrderResponse {
  orderId: string;
  status: OrderStatus;
  acceptedAtUtc: string;
  remainingQuantity: number;
}

export interface DemoHolding {
  symbol: string;
  quantity: number;
}

export interface BootstrapDemoAccountRequest {
  displayName?: string;
  email?: string;
}

export interface ResolveAccountsRequest {
  userIds: string[];
}

export interface DemoAccount {
  userId: string;
  displayName: string;
  email: string;
  cashAvailable: number;
  holdings: DemoHolding[];
}

export interface AccountIdentity {
  userId: string;
  displayName: string;
  email: string;
}

export interface AccountSnapshot {
  userId: string;
  displayName: string;
  email: string;
  cashAvailable: number;
  holdings: DemoHolding[];
}

export type FundsDisplayState =
  | "loading"
  | "confirmed"
  | "updating"
  | "unavailable";

export interface TradeRecord {
  tradeId: string;
  symbol: string;
  price: number;
  quantity: number;
  executedAtUtc: string;
  buyerUserId: string;
  sellerUserId: string;
}

export interface SettlementRecord {
  settlementId: string;
  tradeId: string;
  status: SettlementStatus;
  startedAtUtc: string;
  completedAtUtc: string | null;
  failureReason: string | null;
}

export interface RealtimeEnvelope<TPayload> {
  version: number;
  payload: TPayload;
}

export interface FundsUpdatedPayload {
  userId: string;
  cashAvailable: number;
  changedAtUtc: string;
}

export type OrderBookUpdatedMessage = RealtimeEnvelope<OrderBookSnapshot>;
export type TradeRecordedMessage = RealtimeEnvelope<TradeRecord>;
export type FundsUpdatedMessage = RealtimeEnvelope<FundsUpdatedPayload>;
export type SettlementUpdatedMessage = RealtimeEnvelope<
  SettlementRecord & {
    symbol?: string;
    userId?: string;
  }
>;
