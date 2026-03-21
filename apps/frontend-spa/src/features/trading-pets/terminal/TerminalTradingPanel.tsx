import {
  type FormEvent,
  useCallback,
  useEffect,
  useId,
  useState,
} from "react";
import {
  submitTerminalAsk,
  submitTerminalBid,
  submitTerminalBuyNow,
  type TerminalAccountSummaryDto,
  type TerminalMarketRowDto,
} from "../tradingPetsApi";

export type TerminalOrderMode = "bid" | "ask" | "buy-now";

type Outcome =
  | { kind: "success"; message: string }
  | { kind: "error"; message: string };

type Props = {
  marketEntry: TerminalMarketRowDto | null;
  accountSummary: TerminalAccountSummaryDto | null;
  loading: boolean;
  error: string | null;
  accessToken?: string;
  onOrderSettled?: () => void | Promise<void>;
};

const parsePositiveInt = (raw: string): number | null => {
  const n = Number.parseInt(raw.trim(), 10);
  if (!Number.isFinite(n) || n < 1) {
    return null;
  }
  return n;
};

const parsePositiveMoney = (raw: string): number | null => {
  const n = Number.parseFloat(raw.trim());
  if (!Number.isFinite(n) || n <= 0) {
    return null;
  }
  return n;
};

export const TerminalTradingPanel = ({
  marketEntry,
  accountSummary,
  loading,
  error,
  accessToken,
  onOrderSettled,
}: Props) => {
  const baseId = useId();
  const [mode, setMode] = useState<TerminalOrderMode>("bid");
  const [quantity, setQuantity] = useState("1");
  const [limitPrice, setLimitPrice] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const [outcome, setOutcome] = useState<Outcome | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    setQuantity("1");
    setLimitPrice("");
    setValidationError(null);
    setOutcome(null);
  }, [marketEntry?.marketEntryId]);

  const clearFeedback = useCallback(() => {
    setValidationError(null);
    setOutcome(null);
  }, []);

  const handleModeChange = (next: TerminalOrderMode) => {
    setMode(next);
    clearFeedback();
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!marketEntry || !accessToken) {
      return;
    }

    setValidationError(null);
    setOutcome(null);

    const qty = parsePositiveInt(quantity);
    if (qty === null) {
      setValidationError("Enter a whole quantity of at least 1.");
      return;
    }

    let limit = 0;
    if (mode !== "buy-now") {
      const price = parsePositiveMoney(limitPrice);
      if (price === null) {
        setValidationError("Enter a limit price greater than zero.");
        return;
      }
      limit = price;
    }

    setSubmitting(true);
    try {
      const marketEntryId = marketEntry.marketEntryId;
      const result =
        mode === "bid"
          ? await submitTerminalBid(
              { marketEntryId, quantity: qty, limitPrice: limit },
              accessToken,
            )
          : mode === "ask"
            ? await submitTerminalAsk(
                { marketEntryId, quantity: qty, limitPrice: limit },
                accessToken,
              )
            : await submitTerminalBuyNow(
                { marketEntryId, quantity: qty },
                accessToken,
              );

      setOutcome({ kind: "success", message: result.message });
      try {
        await onOrderSettled?.();
      } catch {
        /* refresh is best-effort; keep confirmed backend message */
      }
    } catch (err) {
      setOutcome({
        kind: "error",
        message:
          err instanceof Error ? err.message : "Order could not be submitted.",
      });
    } finally {
      setSubmitting(false);
    }
  };

  const canSubmit =
    Boolean(marketEntry && accessToken) && !submitting && !loading;
  const submitDisabled = !canSubmit;

  return (
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

      {marketEntry ? (
        <form
          className="trading-pets-form trading-pets-terminal__order-form"
          onSubmit={(ev) => void handleSubmit(ev)}
          aria-busy={submitting}
          noValidate
        >
          <fieldset className="trading-pets-terminal__mode-fieldset">
            <legend className="muted small">Order type</legend>
            <div className="trading-pets-field trading-pets-field--inline">
              <label htmlFor={`${baseId}-mode-bid`}>
                <input
                  id={`${baseId}-mode-bid`}
                  type="radio"
                  name={`${baseId}-mode`}
                  checked={mode === "bid"}
                  onChange={() => handleModeChange("bid")}
                />{" "}
                Bid
              </label>
              <label htmlFor={`${baseId}-mode-ask`}>
                <input
                  id={`${baseId}-mode-ask`}
                  type="radio"
                  name={`${baseId}-mode`}
                  checked={mode === "ask"}
                  onChange={() => handleModeChange("ask")}
                />{" "}
                Ask
              </label>
              <label htmlFor={`${baseId}-mode-buynow`}>
                <input
                  id={`${baseId}-mode-buynow`}
                  type="radio"
                  name={`${baseId}-mode`}
                  checked={mode === "buy-now"}
                  onChange={() => handleModeChange("buy-now")}
                />{" "}
                Buy now
              </label>
            </div>
          </fieldset>

          <div className="trading-pets-field">
            <label htmlFor={`${baseId}-qty`}>Quantity</label>
            <input
              id={`${baseId}-qty`}
              type="number"
              inputMode="numeric"
              min={1}
              step={1}
              value={quantity}
              onChange={(ev) => {
                setQuantity(ev.target.value);
                clearFeedback();
              }}
              disabled={submitting}
            />
          </div>

          {mode !== "buy-now" ? (
            <div className="trading-pets-field">
              <label htmlFor={`${baseId}-price`}>Limit price ($)</label>
              <input
                id={`${baseId}-price`}
                type="number"
                inputMode="decimal"
                min={0}
                step="0.01"
                value={limitPrice}
                onChange={(ev) => {
                  setLimitPrice(ev.target.value);
                  clearFeedback();
                }}
                disabled={submitting}
              />
            </div>
          ) : null}

          {validationError ? (
            <p className="trading-pets-error small" role="alert">
              {validationError}
            </p>
          ) : null}

          {outcome ? (
            <div
              className={
                outcome.kind === "success"
                  ? "trading-pets-terminal__outcome trading-pets-terminal__outcome--success"
                  : "trading-pets-terminal__outcome trading-pets-terminal__outcome--error"
              }
              role={outcome.kind === "success" ? "status" : "alert"}
            >
              {outcome.message}
            </div>
          ) : null}

          <button
            type="submit"
            className="primary-button"
            disabled={submitDisabled}
          >
            {submitting ? "Submitting…" : "Submit order"}
          </button>
        </form>
      ) : null}
    </section>
  );
};
