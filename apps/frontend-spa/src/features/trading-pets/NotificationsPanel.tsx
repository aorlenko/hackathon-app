import { useEffect, useMemo, useState } from "react";
import { getNotifications, type NotificationDto } from "./tradingPetsApi";
import { notificationLabel, notificationVariant } from "./notificationPresentation";

type Props = {
  traderId: string;
  accessToken?: string;
  reloadToken: number;
};

function byCreatedAtDesc(a: NotificationDto, b: NotificationDto): number {
  return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
}

export const NotificationsPanel = ({ traderId, accessToken, reloadToken }: Props) => {
  const [rows, setRows] = useState<NotificationDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const sorted = useMemo(() => [...rows].sort(byCreatedAtDesc), [rows]);

  const load = async () => {
    setError(null);
    setLoading(true);
    try {
      const next = await getNotifications(traderId, accessToken);
      setRows(next);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load recent activity");
    } finally {
      setLoading(false);
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
    <section className="trading-pets-card trading-pets-activity">
      <header className="trading-pets-card__header">
        <h2 id="trading-region-activity-title">Recent activity</h2>
        <p className="muted small">Notifications for bids, sales, and completed trades — newest first.</p>
      </header>
      <div className="trading-pets-card__body trading-pets-activity__body">
        {loading && rows.length === 0 ? (
          <p className="muted small trading-pets-panel-loading">Loading activity…</p>
        ) : null}
        {!loading && sorted.length === 0 && !error ? (
          <p className="trading-pets-empty trading-pets-activity__empty">
            No activity yet. Purchases, new offers for sale, bids, and trades will show up here.
          </p>
        ) : null}
        <ul className="trading-pets-notifications" aria-busy={loading}>
          {sorted.map((n) => {
            const variant = notificationVariant(n.type);
            return (
              <li
                key={n.id}
                className={`trading-pets-notifications__item trading-pets-notifications__item--${variant}`}
              >
                <div className="trading-pets-notifications__title">{notificationLabel(n.type)}</div>
                <div className="muted small">
                  {n.petName}
                  {typeof n.amount === "number" ? ` · $${n.amount.toFixed(2)}` : ""} · with{" "}
                  {n.counterpartyDisplayName}
                </div>
                <div className="muted small">{new Date(n.createdAt).toLocaleString()}</div>
              </li>
            );
          })}
        </ul>
        {error ? <p className="trading-pets-error trading-pets-error--soft">{error}</p> : null}
      </div>
    </section>
  );
};
