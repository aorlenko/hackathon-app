import type { SettlementRecord, TradeRecord } from "../../contracts/trading";

interface LifecycleTraceTableProps {
  trades: TradeRecord[];
  settlements: SettlementRecord[];
}

const settlementByTradeId = (settlements: SettlementRecord[]) =>
  new Map(settlements.map((settlement) => [settlement.tradeId, settlement]));

export const LifecycleTraceTable = ({
  trades,
  settlements,
}: LifecycleTraceTableProps) => {
  const settlementMap = settlementByTradeId(settlements);

  return (
    <section className="card">
      <div className="section-header">
        <h2>Lifecycle trace</h2>
        <span className="tag">{trades.length}</span>
      </div>
      {trades.length === 0 ? (
        <p className="muted">No lifecycle records available yet.</p>
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              <th>Trade</th>
              <th>Symbol</th>
              <th>Execution</th>
              <th>Quantity</th>
              <th>Settlement</th>
              <th>Completed</th>
            </tr>
          </thead>
          <tbody>
            {trades.map((trade) => {
              const settlement = settlementMap.get(trade.tradeId);

              return (
                <tr key={trade.tradeId}>
                  <td>{trade.tradeId}</td>
                  <td>{trade.symbol}</td>
                  <td>{new Date(trade.executedAtUtc).toLocaleString()}</td>
                  <td>{trade.quantity}</td>
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
