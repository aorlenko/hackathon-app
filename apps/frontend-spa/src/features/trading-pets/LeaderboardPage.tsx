import { useCallback, useEffect, useRef, useState } from "react";
import { useTradingAuth } from "../auth/AuthProvider";
import { getLeaderboard, type LeaderboardRowDto } from "./tradingPetsApi";
import { useMyPetTrader } from "./MyPetTraderContext";

export const LeaderboardPage = () => {
  const auth = useTradingAuth();
  const { hubInvalidateSeq } = useMyPetTrader();
  const [rows, setRows] = useState<LeaderboardRowDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const skipNextHubInvalidateEffect = useRef(true);

  const load = useCallback(async () => {
    setError(null);
    try {
      setRows(await getLeaderboard(auth.accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load leaderboard");
    }
  }, [auth.accessToken]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (skipNextHubInvalidateEffect.current) {
      skipNextHubInvalidateEffect.current = false;
      return;
    }
    void load();
  }, [hubInvalidateSeq, load]);

  return (
    <div className="trading-pets-page">
      <header className="trading-pets-page__header">
        <h1>Leaderboard</h1>
        <p className="muted">Ranked by total portfolio value (cash + locks + intrinsic holdings).</p>
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
