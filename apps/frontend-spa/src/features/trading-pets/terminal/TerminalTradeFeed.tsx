import type { TerminalRecentTradeDto } from "../tradingPetsApi";

const money = (n: number) => `$${n.toFixed(2)}`;

type Props = {
  marketLabel: string | null;
  trades: TerminalRecentTradeDto[];
  newTradeIds: string[];
  loading: boolean;
  error: string | null;
};

export const TerminalTradeFeed = ({
  marketLabel,
  trades,
  newTradeIds,
  loading,
  error,
}: Props) => {
  const newTradeIdSet = new Set(newTradeIds);

  return (
    <section
      className="trading-pets-card trading-pets-terminal__tape"
      aria-label="Recent trades"
    >
      <header className="trading-pets-card__header">
        <h2>Recent trades</h2>
        {marketLabel ? (
          <p className="muted small">{marketLabel}</p>
        ) : (
          <p className="muted small">Tape for the selected market.</p>
        )}
      </header>
      {error ? (
        <p className="trading-pets-error small" role="alert">
          {error}
          {trades.length > 0 ? (
            <span className="muted"> Showing last trade list.</span>
          ) : null}
        </p>
      ) : null}
      {loading && trades.length === 0 ? (
        <p className="trading-pets-loading muted">Loading trades…</p>
      ) : null}
      {loading && trades.length > 0 ? (
        <p className="muted small">Refreshing recent prints…</p>
      ) : null}
      {!loading && !marketLabel ? (
        <p className="trading-pets-empty">No market selected.</p>
      ) : null}
      {marketLabel && trades.length === 0 && !loading ? (
        <p className="trading-pets-empty">No recent trades.</p>
      ) : null}
      {trades.length > 0 ? (
        <ul className="trading-pets-list trading-pets-terminal__scroll trading-pets-terminal__tape-list">
          {trades.map((t) => (
            <li
              key={t.tradeId}
              className={
                newTradeIdSet.has(t.tradeId)
                  ? "trading-pets-terminal__trade-row trading-pets-terminal__trade-row--new"
                  : "trading-pets-terminal__trade-row"
              }
            >
              <div className="trading-pets-terminal__tape-row">
                <strong>{money(t.price)}</strong>
                <span className="muted small">
                  × {t.quantity} · {t.executionType}
                </span>
              </div>
              <div className="muted small">
                {new Date(t.executedAt).toLocaleString()}
              </div>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
};
