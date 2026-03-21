export type NotificationVisualVariant =
  | "bid"
  | "trade"
  | "listing"
  | "default";

export function notificationLabel(type: string): string {
  switch (type) {
    case "BidReceived":
      return "Bid received";
    case "BidAccepted":
      return "Bid accepted";
    case "BidRejected":
      return "Bid rejected";
    case "BidWithdrawn":
      return "Bid withdrawn";
    case "Outbid":
      return "Outbid";
    case "ListingRemoved":
      return "No longer for sale";
    case "TradeCompleted":
      return "Trade completed";
    default:
      return type;
  }
}

export function notificationVariant(type: string): NotificationVisualVariant {
  switch (type) {
    case "BidReceived":
    case "BidAccepted":
    case "BidRejected":
    case "BidWithdrawn":
    case "Outbid":
      return "bid";
    case "TradeCompleted":
      return "trade";
    case "ListingRemoved":
      return "listing";
    default:
      return "default";
  }
}
