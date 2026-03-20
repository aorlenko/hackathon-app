import { useEffect, useState } from "react";
import { getNotifications, type NotificationDto } from "./tradingPetsApi";

type Props = {
  traderId: string;
  accessToken?: string;
  reloadToken: number;
};

const labelForType = (type: string) => {
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
      return "Listing removed";
    case "TradeCompleted":
      return "Trade completed";
    default:
      return type;
  }
};

export const NotificationsPanel = ({ traderId, accessToken, reloadToken }: Props) => {
  const [rows, setRows] = useState<NotificationDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setError(null);
    try {
      const next = await getNotifications(traderId, accessToken);
      setRows(next);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load notifications");
    }
  };

  useEffect(() => {
    if (!traderId || !accessToken) {
      return;
    }
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [traderId, accessToken, reloadToken]);

  return (
    <section className="trading-pets-card">
      <header className="trading-pets-card__header">
        <h2>Notifications</h2>
      </header>
      <ul className="trading-pets-notifications">
        {rows.map((n) => (
          <li key={n.id}>
            <div className="trading-pets-notifications__title">
              {labelForType(n.type)}
            </div>
            <div className="muted small">
              {n.petName}
              {typeof n.amount === "number" ? ` · $${n.amount.toFixed(2)}` : ""} · with{" "}
              {n.counterpartyDisplayName}
            </div>
            <div className="muted small">{new Date(n.createdAt).toLocaleString()}</div>
          </li>
        ))}
      </ul>
      {error ? <p className="trading-pets-error">{error}</p> : null}
    </section>
  );
};
