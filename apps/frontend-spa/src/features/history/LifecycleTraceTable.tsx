import type {
  AccountIdentity,
  SettlementRecord,
  TradeRecord,
} from "../../contracts/trading";
import { formatParticipantLabel } from "../account/useResolvedAccountIdentities";

interface LifecycleTraceTableProps {
  trades: TradeRecord[];
  settlements: SettlementRecord[];
  identities: Map<string, AccountIdentity>;
  currentUserId?: string | null;
  currentUserEmail?: string | null;
}

const settlementByTradeId = (settlements: SettlementRecord[]) =>
  new Map(settlements.map((settlement) => [settlement.tradeId, settlement]));

export const LifecycleTraceTable = ({
  trades,
  settlements,
  identities,
  currentUserId,
  currentUserEmail,
}: LifecycleTraceTableProps) => {
  const settlementMap = settlementByTradeId(settlements);

  return (
    <section className="trading-pets-card">
      <div className="section-header">
        <h2>Lifecycle trace</h2>
        <span className="tag">{trades.length}</span>
      </div>
      {trades.length === 0 ? (
        <p className="muted">No lifecycle records available yet.</p>
      ) : (
        <table className="trading-pets-table">
          <thead>
            <tr>
              <th>Execution</th>
              <th>Symbol</th>
              <th>Quantity</th>
              <th>Buyer</th>
              <th>Seller</th>
              <th>Settlement</th>
              <th>Completed</th>
            </tr>
          </thead>
          <tbody>
            {trades.map((trade) => {
              const settlement = settlementMap.get(trade.tradeId);

              return (
                <tr key={trade.tradeId}>
                  <td>{new Date(trade.executedAtUtc).toLocaleString()}</td>
                  <td>{trade.symbol}</td>
                  <td>{trade.quantity}</td>
                  <td>
                    {formatParticipantLabel(trade.buyerUserId, identities, {
                      currentUserId,
                      currentUserEmail,
                      fallbackLabel: "Buyer",
                    })}
                  </td>
                  <td>
                    {formatParticipantLabel(trade.sellerUserId, identities, {
                      currentUserId,
                      currentUserEmail,
                      fallbackLabel: "Seller",
                    })}
                  </td>
                  <td>{settlement?.status ?? "PENDING"}</td>
                  <td>
                    {settlement?.completedAtUtc
                      ? new Date(settlement.completedAtUtc).toLocaleString()
                      : "-"}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </section>
  );
};
