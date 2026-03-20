import type { TerminalAccountSummaryDto, TerminalMarketRowDto } from "../tradingPetsApi";

type Props = {
  marketEntry: TerminalMarketRowDto | null;
  accountSummary: TerminalAccountSummaryDto | null;
  loading: boolean;
  error: string | null;
};

export const TerminalTradingPanel = ({
  marketEntry,
  accountSummary,
  loading,
  error,
}: Props) => (
  <section
    className="trading-pets-card trading-pets-terminal__panel"
    aria-label="Account and trading"
  >
    <header className="trading-pets-card__header">
      <h2>Trading</h2>
      {marketEntry ? (
        <p className="muted small">{marketEntry.displayName}</p>
      ) : (
        <p className="muted small">Workspace account snapshot.</p>
      )}
    </header>
    {error ? (
      <p className="trading-pets-error small" role="alert">
        {error}
        {accountSummary ? (
          <span className="muted"> Showing last account snapshot.</span>
        ) : null}
      </p>
    ) : null}
    {loading && !accountSummary ? (
      <p className="trading-pets-loading muted">Loading account…</p>
    ) : null}
    {!loading && !marketEntry ? (
      <p className="trading-pets-empty">No market selected.</p>
    ) : null}
    {accountSummary ? (
      <div className="trading-pets-summary trading-pets-terminal__account">
        <div>
          <div className="muted small">Trader</div>
          <strong>{accountSummary.displayName}</strong>
        </div>
        <div>
          <div className="muted small">Available cash</div>
          <strong>${accountSummary.availableCash.toFixed(2)}</strong>
        </div>
        <div>
          <div className="muted small">Locked cash</div>
          <strong>${accountSummary.lockedCash.toFixed(2)}</strong>
        </div>
        <div>
          <div className="muted small">Portfolio</div>
          <strong>${accountSummary.portfolioTotal.toFixed(2)}</strong>
        </div>
        <div>
          <div className="muted small">Owned (this market)</div>
          <strong>{accountSummary.ownedQuantity}</strong>
        </div>
        <div>
          <div className="muted small">Eligible to list</div>
          <strong>{accountSummary.eligibleAskQuantity}</strong>
        </div>
      </div>
    ) : null}
    <p className="muted small trading-pets-inline">
      Bid, ask, and buy-now actions are not available in this read-only workspace
      yet.
    </p>
  </section>
);
