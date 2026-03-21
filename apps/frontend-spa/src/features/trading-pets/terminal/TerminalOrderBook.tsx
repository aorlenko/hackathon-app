import type { TerminalOrderBookDto, TerminalMarketRowDto } from "../tradingPetsApi";
import { getOrderBookLevelKey, type TerminalOrderBookLevelHighlight } from "./terminalHighlights";

const money = (n: number) => `$${n.toFixed(2)}`;

type Props = {
  marketEntry: TerminalMarketRowDto | null;
  orderBook: TerminalOrderBookDto | null;
  bidHighlights: Record<string, TerminalOrderBookLevelHighlight>;
  askHighlights: Record<string, TerminalOrderBookLevelHighlight>;
  loading: boolean;
  error: string | null;
};

const levelClassName = (
  side: "bid" | "ask",
  highlight?: TerminalOrderBookLevelHighlight,
) => {
  if (!highlight) {
    return `trading-pets-terminal__book-row trading-pets-terminal__book-row--${side}`;
  }

  return `trading-pets-terminal__book-row trading-pets-terminal__book-row--${side} trading-pets-terminal__book-row--${highlight.change}`;
};

export const TerminalOrderBook = ({
  marketEntry,
  orderBook,
  bidHighlights,
  askHighlights,
  loading,
  error,
}: Props) => (
  <section
    className="trading-pets-card trading-pets-terminal__book"
    aria-label="Order book"
  >
    <header className="trading-pets-card__header">
      <h2>Order book</h2>
      {marketEntry ? (
        <p className="muted small">{marketEntry.displayName}</p>
      ) : (
        <p className="muted small">Select a market to view depth.</p>
      )}
    </header>
    {error ? (
      <p className="trading-pets-error small" role="alert">
        {error}
        {orderBook ? (
          <span className="muted"> Showing last captured book.</span>
        ) : null}
      </p>
    ) : null}
    {loading && !orderBook ? (
      <p className="trading-pets-loading muted">Loading order book…</p>
    ) : null}
    {loading && orderBook ? (
      <p className="muted small">Refreshing depth…</p>
    ) : null}
    {!loading && !marketEntry ? (
      <p className="trading-pets-empty">No market selected.</p>
    ) : null}
    {marketEntry && orderBook ? (
      <>
        <p className="muted small">
          Captured {new Date(orderBook.capturedAt).toLocaleString()}
        </p>
        <div className="trading-pets-terminal__book-columns">
          <div>
            <h3 className="trading-pets-subheading trading-pets-terminal__side trading-pets-terminal__side--bid">
              Bids
            </h3>
            {orderBook.bids.length === 0 ? (
              <p className="trading-pets-empty">No bids</p>
            ) : (
              <table className="trading-pets-table">
                <thead>
                  <tr>
                    <th scope="col">Price</th>
                    <th scope="col">Qty</th>
                    <th scope="col">Orders</th>
                  </tr>
                </thead>
                <tbody>
                  {orderBook.bids.map((r, i) => (
                    <tr
                      key={`${r.price}-${i}`}
                      className={levelClassName(
                        "bid",
                        bidHighlights[getOrderBookLevelKey(r)],
                      )}
                    >
                      <td>{money(r.price)}</td>
                      <td>{r.quantity}</td>
                      <td>{r.orderCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
          <div>
            <h3 className="trading-pets-subheading trading-pets-terminal__side trading-pets-terminal__side--ask">
              Asks
            </h3>
            {orderBook.asks.length === 0 ? (
              <p className="trading-pets-empty">No asks</p>
            ) : (
              <table className="trading-pets-table">
                <thead>
                  <tr>
                    <th scope="col">Price</th>
                    <th scope="col">Qty</th>
                    <th scope="col">Orders</th>
                  </tr>
                </thead>
                <tbody>
                  {orderBook.asks.map((r, i) => (
                    <tr
                      key={`${r.price}-${i}`}
                      className={levelClassName(
                        "ask",
                        askHighlights[getOrderBookLevelKey(r)],
                      )}
                    >
                      <td>{money(r.price)}</td>
                      <td>{r.quantity}</td>
                      <td>{r.orderCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      </>
    ) : null}
  </section>
);
