import { useEffect, useMemo, useState } from "react";
import type { BreedDto } from "./tradingPetsApi";
import { getBreeds, purchasePets } from "./tradingPetsApi";

type Props = {
  traderId: string;
  accessToken?: string;
  /** Bumped when SignalR (or poll) invalidates workspace data so breed supply stays in sync. */
  reloadToken: number;
  onPurchased: () => void;
};

export const PrimaryMarketPanel = ({
  traderId,
  accessToken,
  reloadToken,
  onPurchased,
}: Props) => {
  const [breeds, setBreeds] = useState<BreedDto[]>([]);
  const [breedId, setBreedId] = useState<string>("");
  const [quantity, setQuantity] = useState(1);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const selected = useMemo(
    () => breeds.find((b) => b.id === breedId),
    [breeds, breedId],
  );

  const loadBreeds = async () => {
    setError(null);
    setLoading(true);
    try {
      const rows = await getBreeds(accessToken);
      setBreeds(rows);
      if (!breedId && rows[0]) {
        setBreedId(rows[0].id);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load breeds");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!accessToken) {
      return;
    }
    void loadBreeds();
    // eslint-disable-next-line react-hooks/exhaustive-deps -- reloadToken intentionally triggers refetch
  }, [accessToken, reloadToken]);

  const onPurchase = async () => {
    setError(null);
    setLoading(true);
    try {
      await purchasePets({ traderId, breedId, quantity }, accessToken);
      onPurchased();
      await loadBreeds();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Purchase failed");
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="trading-pets-card">
      <header className="trading-pets-card__header">
        <h2>Primary market</h2>
        <p className="muted small">
          Buy new pets from limited supply at the breed retail price.
        </p>
      </header>
      <div className="trading-pets-card__body">
        {breeds.length > 0 ? (
          <div className="trading-pets-form">
            <label className="trading-pets-field">
              <span>Breed</span>
              <select
                value={breedId}
                onChange={(event) => setBreedId(event.target.value)}
              >
                {breeds.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name} — ${b.retailPrice.toFixed(2)} (supply {b.remainingSupply})
                  </option>
                ))}
              </select>
            </label>
            <label className="trading-pets-field">
              <span>Quantity</span>
              <input
                type="number"
                min={1}
                value={quantity}
                onChange={(event) => setQuantity(Number(event.target.value) || 1)}
              />
            </label>
            {selected ? (
              <p className="muted small">
                Estimated cost: ${(selected.retailPrice * quantity).toFixed(2)}
              </p>
            ) : null}
            <button
              type="button"
              className="primary-button"
              disabled={loading || !breedId}
              onClick={() => void onPurchase()}
            >
              Purchase
            </button>
          </div>
        ) : !loading ? (
          <p className="muted small">No breeds returned — check the API or your connection.</p>
        ) : null}
        {error ? <p className="trading-pets-error">{error}</p> : null}
      </div>
    </section>
  );
};
