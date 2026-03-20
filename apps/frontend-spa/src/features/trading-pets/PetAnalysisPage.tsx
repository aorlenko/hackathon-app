import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { useTradingAuth } from "../auth/AuthProvider";
import { useMyPetTrader } from "./MyPetTraderContext";
import { getPetAnalysis } from "./tradingPetsApi";

export const PetAnalysisPage = () => {
  const { petId = "" } = useParams();
  const auth = useTradingAuth();
  const { traderId } = useMyPetTrader();
  const [payload, setPayload] = useState<Record<string, unknown> | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!petId) {
      return;
    }
    let active = true;
    void (async () => {
      setError(null);
      try {
        const data = await getPetAnalysis(petId, traderId, auth.accessToken);
        if (active) {
          setPayload(data);
        }
      } catch (e) {
        if (active) {
          setError(e instanceof Error ? e.message : "Unable to load analysis");
        }
      }
    })();
    return () => {
      active = false;
    };
  }, [auth.accessToken, petId, traderId]);

  return (
    <div className="trading-pets-page">
      <h1>Pet analysis</h1>
      {payload ? (
        <pre className="trading-pets-pre">{JSON.stringify(payload, null, 2)}</pre>
      ) : (
        <p className="muted">Loading…</p>
      )}
      {error ? <p className="trading-pets-error">{error}</p> : null}
    </div>
  );
};
