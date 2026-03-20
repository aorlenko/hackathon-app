import { useTradingAuth } from "../auth/AuthProvider";
import { useAccountFunds } from "./useAccountFunds";

const getStatusLabel = (state: ReturnType<typeof useAccountFunds>["state"]) => {
  switch (state) {
    case "confirmed":
      return "confirmed";
    case "updating":
      return "updating";
    case "unavailable":
      return "unavailable";
    default:
      return "loading";
  }
};

const getHelperText = (
  state: ReturnType<typeof useAccountFunds>["state"],
  error: string,
) => {
  switch (state) {
    case "updating":
      return "Updating";
    case "unavailable":
      return error || "Retry to confirm funds.";
    default:
      return "Loading funds.";
  }
};

export const FundsPanel = () => {
  const auth = useTradingAuth();
  const funds = useAccountFunds({
    accessToken: auth.accessToken,
    initialSnapshot: auth.accountSnapshot,
    isAuthenticated: auth.isAuthenticated,
    userId: auth.userId,
  });

  if (!auth.isAuthenticated) {
    return null;
  }

  return (
    <section className="funds-panel" aria-live="polite">
      <div className="funds-panel__topline">
        <span className="muted small funds-panel__label">Available funds</span>
        <span className={`status-pill funds-pill ${funds.state}`}>
          {getStatusLabel(funds.state)}
        </span>
      </div>
      <div className="funds-panel__content">
        <strong className="funds-panel__amount">
          {funds.formattedFunds ??
            (funds.state === "unavailable" ? "Unavailable" : "Loading funds...")}
        </strong>
      </div>
      {funds.state === "confirmed" ? null : (
        <p
          className={`small funds-panel__helper ${
            funds.state === "unavailable" ? "error-text" : "muted"
          }`}
        >
          {getHelperText(funds.state, funds.error)}
        </p>
      )}
      {funds.state === "unavailable" ? (
        <button
          className="secondary-button funds-panel__retry"
          onClick={() => void funds.refresh()}
          type="button"
        >
          Retry
        </button>
      ) : null}
    </section>
  );
};
