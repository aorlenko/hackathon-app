import { useEffect, useState } from "react";
import {
  createListing,
  getMarketListings,
  placeBid,
  type MarketListingDto,
  type PetSummaryDto,
} from "./tradingPetsApi";
import { ListingSellerActions } from "./ListingSellerActions";

type Props = {
  traderId: string;
  accessToken?: string;
  inventory: PetSummaryDto[];
  onChanged: () => void;
};

export const SecondaryMarketPanel = ({
  traderId,
  accessToken,
  inventory,
  onChanged,
}: Props) => {
  const [listings, setListings] = useState<MarketListingDto[]>([]);
  const [petId, setPetId] = useState<string>("");
  const [ask, setAsk] = useState(50);
  const [bidListingId, setBidListingId] = useState<string>("");
  const [bidAmount, setBidAmount] = useState(25);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setError(null);
    try {
      setListings(await getMarketListings(accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load listings");
    }
  };

  useEffect(() => {
    if (!accessToken || !traderId) {
      return;
    }
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accessToken, traderId]);

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
    try {
      await placeBid(bidListingId, { traderId, amount: bidAmount }, accessToken);
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
        <button type="button" className="secondary-button" onClick={() => void load()}>
          Refresh listings
        </button>
      </header>
      <div className="trading-pets-card__body">
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
          <ul className="trading-pets-list">
            {listings.map((l) => (
              <li key={l.listingId}>
                <div>
                  <strong>{l.breedName}</strong> · ask ${l.askingPrice.toFixed(2)} · seller{" "}
                  {l.sellerDisplayName}
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
                    <button
                      type="button"
                      className="secondary-button"
                      onClick={() => setBidListingId(l.listingId)}
                    >
                      Select for bid
                    </button>
                  </div>
                )}
              </li>
            ))}
          </ul>
        </div>
        <div className="trading-pets-form">
          <h3 className="trading-pets-subheading">Place bid</h3>
          <label className="trading-pets-field">
            <span>Listing</span>
            <input
              value={bidListingId}
              onChange={(e) => setBidListingId(e.target.value)}
              placeholder="listing id"
            />
          </label>
          <label className="trading-pets-field">
            <span>Amount</span>
            <input
              type="number"
              min={1}
              value={bidAmount}
              onChange={(e) => setBidAmount(Number(e.target.value) || 1)}
            />
          </label>
          <button type="button" className="primary-button" onClick={() => void bid()}>
            Submit bid
          </button>
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
