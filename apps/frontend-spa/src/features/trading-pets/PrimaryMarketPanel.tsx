import { useEffect, useMemo, useState } from "react";
import type { BreedDto } from "./tradingPetsApi";
import { getBreeds, purchasePets } from "./tradingPetsApi";

export type PrimaryPurchaseSummary = {
  breedName: string;
  quantity: number;
  totalPrice: number;
};

type Props = {
  traderId: string;
  accessToken?: string;
  /** Bumped when SignalR (or poll) invalidates workspace data so breed supply stays in sync. */
  reloadToken: number;
  onPurchased: (purchase: PrimaryPurchaseSummary) => void | Promise<void>;
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
    if (!selected) {
      setError("Choose a breed with available supply.");
      return;
    }
    if (selected.remainingSupply <= 0) {
      setError("This breed is out of stock — pick another or check back later.");
      return;
    }
    if (quantity > selected.remainingSupply) {
      setError(`Only ${selected.remainingSupply} pet(s) left at retail for this breed.`);
      return;
    }
    setLoading(true);
    try {
      await purchasePets({ traderId, breedId, quantity }, accessToken);
      await onPurchased({
        breedName: selected.name,
        quantity,
        totalPrice: selected.retailPrice * quantity,
      });
      await loadBreeds();
    } catch (e) {
      setError(
        e instanceof Error
          ? e.message
          : "Purchase could not be completed. Check your available cash and try again.",
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="trading-pets-card trading-pets-primary">
      <header className="trading-pets-card__header">
        <h2 id="trading-region-primary-title">Primary supply</h2>
        <p className="muted small">
          New pets from the issuer at the breed <strong>retail price</strong>. Limited <strong>remaining supply</strong>{" "}
          per breed — separate from pets other traders offer for sale.
        </p>
      </header>
      <div className="trading-pets-card__body">
        {loading && breeds.length === 0 ? (
          <p className="muted small trading-pets-panel-loading">Loading breeds and supply…</p>
        ) : null}
        {breeds.length > 0 ? (
          <div className="trading-pets-primary__layout">
            <div className="trading-pets-form trading-pets-primary__controls">
              <label className="trading-pets-field">
                <span>Breed</span>
                <select
                  value={breedId}
                  onChange={(event) => setBreedId(event.target.value)}
                  aria-describedby="trading-primary-supply-hint"
                >
                  {breeds.map((b) => (
                    <option key={b.id} value={b.id}>
                      {b.name}
                    </option>
                  ))}
                </select>
              </label>
              {selected ? (
                <dl className="trading-pets-primary__facts" id="trading-primary-supply-hint">
                  <div>
                    <dt>Retail price (each)</dt>
                    <dd>
                      <strong className="trading-pets-primary__price">${selected.retailPrice.toFixed(2)}</strong>
                    </dd>
                  </div>
                  <div>
                    <dt>Remaining supply</dt>
                    <dd>{selected.remainingSupply}</dd>
                  </div>
                </dl>
              ) : null}
              <label className="trading-pets-field">
                <span>Quantity</span>
                <input
                  type="number"
                  min={1}
                  value={quantity}
                  onChange={(event) => setQuantity(Math.max(1, Number(event.target.value) || 1))}
                />
              </label>
              {selected ? (
                <p className="muted small">
                  Line total at retail: <strong>${(selected.retailPrice * quantity).toFixed(2)}</strong>
                </p>
              ) : null}
              <button
                type="button"
                className="primary-button trading-pets-primary__buy"
                disabled={loading || !breedId || !selected || selected.remainingSupply <= 0}
                onClick={() => void onPurchase()}
              >
                Buy from primary supply
              </button>
            </div>
          </div>
        ) : !loading ? (
          <p className="muted small">No breeds are available right now. Check your connection or try again later.</p>
        ) : null}
        {error ? <p className="trading-pets-error trading-pets-error--soft">{error}</p> : null}
      </div>
    </section>
  );
};
