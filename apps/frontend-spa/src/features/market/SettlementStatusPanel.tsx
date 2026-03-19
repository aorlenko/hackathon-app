import type { SettlementRecord } from "../../contracts/trading";

interface SettlementStatusPanelProps {
  settlements: SettlementRecord[];
}

export const SettlementStatusPanel = ({
  settlements,
}: SettlementStatusPanelProps) => {
  return (
    <section className="card">
      <div className="section-header">
        <h2>Settlement timeline</h2>
        <span className="tag">{settlements.length}</span>
      </div>
      {settlements.length === 0 ? (
        <p className="muted">
          Settlement updates will appear here after matched trades are processed.
        </p>
      ) : (
        <ul className="timeline-list">
          {settlements.map((settlement) => (
            <li key={settlement.settlementId} className="timeline-item">
              <div>
                <strong>{settlement.status}</strong>
                <p className="muted small">Trade {settlement.tradeId}</p>
              </div>
              <div className="timeline-meta">
                <span>
                  Started {new Date(settlement.startedAtUtc).toLocaleString()}
                </span>
                {settlement.completedAtUtc ? (
                  <span>
                    Completed{" "}
                    {new Date(settlement.completedAtUtc).toLocaleString()}
                  </span>
                ) : null}
                {settlement.failureReason ? (
                  <span>Reason: {settlement.failureReason}</span>
                ) : null}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
};
