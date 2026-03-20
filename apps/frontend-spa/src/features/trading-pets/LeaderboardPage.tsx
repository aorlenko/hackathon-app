import { useEffect, useState } from "react";
import { useTradingAuth } from "../auth/AuthProvider";
import { getLeaderboard, type LeaderboardRowDto } from "./tradingPetsApi";

export const LeaderboardPage = () => {
  const auth = useTradingAuth();
  const [rows, setRows] = useState<LeaderboardRowDto[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setError(null);
    try {
      setRows(await getLeaderboard(auth.accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load leaderboard");
    }
  };

  useEffect(() => {
    void load();
  }, [auth.accessToken]);

  return (
    <div className="trading-pets-page">
      <header className="trading-pets-page__header">
        <h1>Leaderboard</h1>
        <p className="muted">Ranked by total portfolio value (cash + locks + intrinsic holdings).</p>
        <button type="button" className="secondary-button" onClick={() => void load()}>
          Refresh
        </button>
      </header>
      <table className="trading-pets-table">
        <thead>
          <tr>
            <th>Rank</th>
            <th>Trader</th>
            <th>Portfolio</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.traderId}>
              <td>{r.rank}</td>
              <td>{r.displayName}</td>
              <td>${r.portfolioTotal.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {error ? <p className="trading-pets-error">{error}</p> : null}
    </div>
  );
};
