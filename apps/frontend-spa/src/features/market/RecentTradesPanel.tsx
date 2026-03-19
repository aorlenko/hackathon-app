import type { TradeRecord } from "../../contracts/trading";

interface RecentTradesPanelProps {
  trades: TradeRecord[];
}

export const RecentTradesPanel = ({ trades }: RecentTradesPanelProps) => {
  return (
    <section className="card">
      <div className="section-header">
        <h2>Recent trades</h2>
        <span className="tag">{trades.length}</span>
      </div>
      {trades.length === 0 ? (
        <p className="muted">No recent trades for this market.</p>
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              <th>Executed</th>
              <th>Price</th>
              <th>Qty</th>
              <th>Buyer</th>
              <th>Seller</th>
            </tr>
          </thead>
          <tbody>
            {trades.map((trade) => (
              <tr key={trade.tradeId}>
                <td>{new Date(trade.executedAtUtc).toLocaleString()}</td>
                <td>{trade.price.toFixed(2)}</td>
                <td>{trade.quantity}</td>
                <td>{trade.buyerUserId}</td>
                <td>{trade.sellerUserId}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
};
