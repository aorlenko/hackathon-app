import { useEffect, useMemo, useState } from "react";
import { useTradingAuth } from "../auth/AuthProvider";
import { useMyPetTrader } from "./MyPetTraderContext";
import { notificationLabel } from "./notificationPresentation";
import { getNotifications, type NotificationDto } from "./tradingPetsApi";

const notificationsPageLimit = 250;

function byCreatedAtDesc(a: NotificationDto, b: NotificationDto): number {
  return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
}

function formatAmount(amount: number | null): string {
  return typeof amount === "number" ? `$${amount.toFixed(2)}` : "—";
}

export const MyNotificationsPage = () => {
  const auth = useTradingAuth();
  const { traderId, hubInvalidateSeq } = useMyPetTrader();
  const [rows, setRows] = useState<NotificationDto[]>([]);
  const [error, setError] = useState<string>("");
  const [loading, setLoading] = useState(true);

  const sortedRows = useMemo(() => [...rows].sort(byCreatedAtDesc), [rows]);

  useEffect(() => {
    if (!traderId || !auth.accessToken) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setError("");

    void getNotifications(traderId, auth.accessToken, notificationsPageLimit)
      .then((response) => {
        setRows(response);
      })
      .catch((requestError: unknown) => {
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Notifications could not be loaded.",
        );
      })
      .finally(() => setLoading(false));
  }, [auth.accessToken, hubInvalidateSeq, traderId]);

  return (
    <div className="trading-pets-page">
      <header className="trading-pets-page__header">
        <h1>My notifications</h1>
        <p className="muted">
          Every bid, listing, and trade notification for your trader account, newest first.
        </p>
      </header>
      {loading ? <p className="trading-pets-loading">Loading notifications...</p> : null}
      {error ? <p className="error-text">{error}</p> : null}
      {!loading && !error && sortedRows.length === 0 ? (
        <p className="muted">No notifications yet.</p>
      ) : null}
      {sortedRows.length > 0 ? (
        <div className="trading-pets-table-scroll">
          <table className="trading-pets-table">
            <thead>
              <tr>
                <th>Received</th>
                <th>Type</th>
                <th>Pet</th>
                <th>Amount</th>
                <th>Counterparty</th>
              </tr>
            </thead>
            <tbody>
              {sortedRows.map((notification) => (
                <tr key={notification.id}>
                  <td>{new Date(notification.createdAt).toLocaleString()}</td>
                  <td>{notificationLabel(notification.type)}</td>
                  <td>{notification.petName}</td>
                  <td>{formatAmount(notification.amount)}</td>
                  <td>{notification.counterpartyDisplayName}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  );
};
