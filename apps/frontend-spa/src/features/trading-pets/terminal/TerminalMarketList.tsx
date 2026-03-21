import type { TerminalMarketRowDto } from "../tradingPetsApi";
import type { TerminalMarketHighlight } from "./terminalHighlights";

const money = (n: number | null | undefined) =>
  n == null ? "—" : `$${n.toFixed(2)}`;

const trendArrow = (d: string) => {
  if (d === "Up") {
    return "↑";
  }
  if (d === "Down") {
    return "↓";
  }
  return "→";
};

type Props = {
  markets: TerminalMarketRowDto[];
  highlights: Record<string, TerminalMarketHighlight>;
  selectedMarketEntryId: string | null;
  onSelect: (marketEntryId: string) => void;
  loading: boolean;
  error: string | null;
};

const flashClass = (direction?: "up" | "down") =>
  direction ? ` trading-pets-terminal__flash trading-pets-terminal__flash--${direction}` : "";

export const TerminalMarketList = ({
  markets,
  highlights,
  selectedMarketEntryId,
  onSelect,
  loading,
  error,
}: Props) => (
  <section
    className="trading-pets-card trading-pets-terminal__markets"
    aria-label="Markets"
  >
    <header className="trading-pets-card__header">
      <h2>Markets</h2>
      <p className="muted small">Select a breed market.</p>
    </header>
    {error ? (
      <p className="trading-pets-error small" role="alert">
        {error}
        {markets.length > 0 ? (
          <span className="muted"> Showing last loaded markets.</span>
        ) : null}
      </p>
    ) : null}
    {loading && markets.length === 0 ? (
      <p className="trading-pets-loading muted">Loading markets…</p>
    ) : null}
    {loading && markets.length > 0 ? (
      <p className="muted small">Refreshing market tape…</p>
    ) : null}
    {!loading && markets.length === 0 && !error ? (
      <p className="trading-pets-empty">No markets available.</p>
    ) : null}
    <ul className="trading-pets-list trading-pets-list--market trading-pets-terminal__scroll">
      {markets.map((m) => {
        const selected = m.marketEntryId === selectedMarketEntryId;
        const highlight = highlights[m.marketEntryId];
        const rowClassName = [
          "trading-pets-terminal__market-row",
          selected ? "trading-pets-list__item--selected" : "",
          highlight ? "trading-pets-terminal__market-row--active" : "",
        ]
          .filter(Boolean)
          .join(" ");

        return (
          <li key={m.marketEntryId}>
            <button
              type="button"
              className={rowClassName}
              onClick={() => onSelect(m.marketEntryId)}
              aria-current={selected ? "true" : undefined}
            >
              <div className="trading-pets-terminal__market-title">
                <strong>{m.displayName}</strong>
                <span
                  className={`muted small${highlight?.trendChanged ? " trading-pets-terminal__trend trading-pets-terminal__trend--active" : ""}`}
                  title="Trend"
                >
                  {trendArrow(m.trendDirection)}
                </span>
              </div>
              <div className="muted small trading-pets-terminal__market-meta">
                <span>Supply {m.currentSupply}</span>
                <span className={flashClass(highlight?.latestTradePrice)}>
                  Last {money(m.latestTradePrice)}
                </span>
              </div>
              <div className="muted small trading-pets-terminal__market-meta">
                <span className={flashClass(highlight?.bestBidPrice)}>
                  Bid {money(m.bestBidPrice)}
                </span>
                <span className={flashClass(highlight?.bestAskPrice)}>
                  Ask {money(m.bestAskPrice)}
                </span>
              </div>
            </button>
          </li>
        );
      })}
    </ul>
  </section>
);
