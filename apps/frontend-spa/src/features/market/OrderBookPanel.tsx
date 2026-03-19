import type { OrderBookLevel, OrderBookSnapshot } from "../../contracts/trading";

interface OrderBookPanelProps {
  orderBook: OrderBookSnapshot | null;
  connectionState: string;
}

const OrderBookTable = ({
  title,
  levels,
}: {
  title: string;
  levels: OrderBookLevel[];
}) => (
  <div>
    <h3>{title}</h3>
    {levels.length === 0 ? (
      <p className="muted">No open orders.</p>
    ) : (
      <table className="data-table">
        <thead>
          <tr>
            <th>Price</th>
            <th>Quantity</th>
            <th>Orders</th>
          </tr>
        </thead>
        <tbody>
          {levels.map((level) => (
            <tr key={`${title}-${level.price}`}>
              <td>{level.price.toFixed(2)}</td>
              <td>{level.quantity}</td>
              <td>{level.orderCount}</td>
            </tr>
          ))}
        </tbody>
      </table>
    )}
  </div>
);

export const OrderBookPanel = ({
  orderBook,
  connectionState,
}: OrderBookPanelProps) => {
  return (
    <section className="card">
      <div className="section-header">
        <h2>Order book</h2>
        <span className="status-pill">{connectionState}</span>
      </div>
      {orderBook ? (
        <>
          <p className="muted small">
            Last updated {new Date(orderBook.lastUpdatedAtUtc).toLocaleString()}
          </p>
          <div className="two-column-grid">
            <OrderBookTable title="Bids" levels={orderBook.bids} />
            <OrderBookTable title="Asks" levels={orderBook.asks} />
          </div>
        </>
      ) : (
        <p className="muted">No market snapshot available yet.</p>
      )}
    </section>
  );
};
