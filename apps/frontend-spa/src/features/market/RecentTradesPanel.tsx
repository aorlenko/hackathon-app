import { formatParticipantLabel } from "../account/useResolvedAccountIdentities";
import type { AccountIdentity, TradeRecord } from "../../contracts/trading";

interface RecentTradesPanelProps {
  trades: TradeRecord[];
  identities: Map<string, AccountIdentity>;
  currentUserId?: string | null;
  currentUserEmail?: string | null;
  currentUserDisplayName?: string | null;
}

export const RecentTradesPanel = ({
  trades,
  identities,
  currentUserId,
  currentUserEmail,
  currentUserDisplayName,
}: RecentTradesPanelProps) => {
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
                <td>
                  {formatParticipantLabel(trade.buyerUserId, identities, {
                    currentUserId,
                    currentUserEmail,
                    currentUserDisplayName,
                    fallbackLabel: "Buyer",
                  })}
                </td>
                <td>
                  {formatParticipantLabel(trade.sellerUserId, identities, {
                    currentUserId,
                    currentUserEmail,
                    currentUserDisplayName,
                    fallbackLabel: "Seller",
                  })}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
};
