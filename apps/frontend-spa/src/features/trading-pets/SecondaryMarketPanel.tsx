import { useCallback, useEffect, useMemo, useState } from "react";
import {
  createListing,
  getMarketListings,
  placeBid,
  type MarketListingDto,
  type PetSummaryDto,
} from "./tradingPetsApi";
import { ListingSellerActions } from "./ListingSellerActions";
import { listingSellerClause } from "./listingSellerLabel";

type Props = {
  traderId: string;
  accessToken?: string;
  reloadToken: number;
  inventory: PetSummaryDto[];
  onChanged: () => void;
};

export const SecondaryMarketPanel = ({
  traderId,
  accessToken,
  reloadToken,
  inventory,
  onChanged,
}: Props) => {
  const [listings, setListings] = useState<MarketListingDto[]>([]);
  const [petId, setPetId] = useState<string>("");
  const [ask, setAsk] = useState(50);
  const [bidListingId, setBidListingId] = useState<string>("");
  const [bidAmount, setBidAmount] = useState(25);
  const [error, setError] = useState<string | null>(null);

  const selectedListing = useMemo(
    () => listings.find((l) => l.listingId === bidListingId) ?? null,
    [listings, bidListingId],
  );

  useEffect(() => {
    if (bidListingId && !listings.some((l) => l.listingId === bidListingId)) {
      setBidListingId("");
    }
  }, [listings, bidListingId]);

  const load = useCallback(async () => {
    setError(null);
    try {
      setListings(await getMarketListings(accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load listings");
    }
  }, [accessToken]);

  useEffect(() => {
    if (!accessToken || !traderId) {
      return;
    }
    void load();
  }, [accessToken, traderId, reloadToken, load]);

  const listPet = async () => {
    setError(null);
    const trimmedPetId = petId.trim();
    if (!trimmedPetId) {
      setError("Choose a pet from your inventory before creating a listing.");
      return;
    }

    try {
      await createListing(
        { traderId, petId: trimmedPetId, askingPrice: ask },
        accessToken,
      );
      onChanged();
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not create listing");
    }
  };

  const bid = async () => {
    setError(null);
    if (!selectedListing) {
      setError("Choose a listing to bid on.");
      return;
    }
    try {
      await placeBid(selectedListing.listingId, { traderId, amount: bidAmount }, accessToken);
      setBidListingId("");
      onChanged();
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not place bid");
    }
  };

  return (
    <section className="trading-pets-card">
      <header className="trading-pets-card__header">
        <h2>Secondary market</h2>
      </header>
      <div className="trading-pets-card__body">
        <p className="muted small">
          A <strong>listing</strong> is a pet from inventory offered for resale at an asking price. Other traders can
          pay the ask or place a bid for you to accept.
        </p>
        <div className="trading-pets-form">
          <h3 className="trading-pets-subheading">List a pet</h3>
          <label className="trading-pets-field">
            <span>Pet</span>
            <select value={petId} onChange={(e) => setPetId(e.target.value)}>
              <option value="">Select…</option>
              {inventory.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.breedName} · intrinsic ${p.intrinsicValue.toFixed(2)}
                </option>
              ))}
            </select>
          </label>
          <label className="trading-pets-field">
            <span>Asking price</span>
            <input
              type="number"
              min={1}
              value={ask}
              onChange={(e) => setAsk(Number(e.target.value) || 1)}
            />
          </label>
          <button
            type="button"
            className="primary-button"
            disabled={!petId.trim() || inventory.length === 0}
            onClick={() => void listPet()}
          >
            Create listing
          </button>
          {inventory.length === 0 ? (
            <p className="muted small">Buy a pet on the primary market first — then you can list it here.</p>
          ) : null}
        </div>
        <div className="trading-pets-form">
          <h3 className="trading-pets-subheading">Open listings</h3>
          <p className="muted small">Everyone&apos;s active resale offers (including yours).</p>
          <ul className="trading-pets-list">
            {listings.map((l) => {
              const sellerClause = listingSellerClause(
                l.sellerTraderId,
                l.sellerDisplayName,
                l.sellerEmail,
                traderId,
              );
              const isBidSelected = l.listingId === bidListingId;
              return (
              <li
                key={l.listingId}
                className={isBidSelected ? "trading-pets-list__item--selected" : undefined}
              >
                <div>
                  <strong>{l.breedName}</strong> · ask ${l.askingPrice.toFixed(2)}
                  {sellerClause ? <> · {sellerClause}</> : null}
                </div>
                <div className="muted small">
                  Recent trade (breed):{" "}
                  {l.recentTradePriceForBreed == null
                    ? "—"
                    : `$${l.recentTradePriceForBreed.toFixed(2)}`}{" "}
                  · New supply: {l.remainingNewSupplyForBreed}
                </div>
                {l.sellerTraderId === traderId ? (
                  <ListingSellerActions
                    listingId={l.listingId}
                    sellerTraderId={traderId}
                    accessToken={accessToken}
                    onChanged={() => {
                      void load();
                      onChanged();
                    }}
                  />
                ) : (
                  <div className="trading-pets-inline">
                    {isBidSelected ? (
                      <span className="muted small" aria-current="true">
                        Selected — use <strong>Place bid</strong> below
                      </span>
                    ) : (
                      <button
                        type="button"
                        className="secondary-button"
                        onClick={() => {
                          setBidListingId(l.listingId);
                          setBidAmount(Math.max(1, Math.round(l.askingPrice * 100) / 100));
                        }}
                      >
                        Select for bid
                      </button>
                    )}
                  </div>
                )}
              </li>
            );
            })}
          </ul>
        </div>
        <div className="trading-pets-form">
          <h3 className="trading-pets-subheading">Place bid</h3>
          {selectedListing ? (
            <>
              <div className="trading-pets-bid-target" aria-live="polite">
                <div className="trading-pets-bid-target__row">
                  <span className="trading-pets-bid-target__label">You&apos;re bidding on</span>
                  <button
                    type="button"
                    className="inline-link trading-pets-bid-target__change"
                    onClick={() => setBidListingId("")}
                  >
                    Change
                  </button>
                </div>
                <p className="trading-pets-bid-target__summary">
                  <strong>{selectedListing.breedName}</strong>
                  <span className="muted"> · asking </span>
                  ${selectedListing.askingPrice.toFixed(2)}
                </p>
              </div>
              <label className="trading-pets-field">
                <span>Your bid amount</span>
                <input
                  type="number"
                  min={1}
                  step={0.01}
                  value={bidAmount}
                  onChange={(e) => setBidAmount(Number(e.target.value) || 1)}
                />
              </label>
              <button type="button" className="primary-button" onClick={() => void bid()}>
                Submit bid
              </button>
            </>
          ) : (
            <p className="muted small">
              Use <strong>Select for bid</strong> on a listing above, then set your amount here.
            </p>
          )}
          <p className="muted small">
            Bids at or above the ask execute immediately; lower bids lock cash until accepted,
            rejected, withdrawn, or replaced.
          </p>
        </div>
        {error ? <p className="trading-pets-error">{error}</p> : null}
      </div>
    </section>
  );
};
