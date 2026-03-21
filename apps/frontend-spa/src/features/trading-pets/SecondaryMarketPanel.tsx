import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  createListing,
  getMarketListings,
  placeBid,
  type MarketListingDto,
  type PetSummaryDto,
} from "./tradingPetsApi";
import { ListingSellerActions } from "./ListingSellerActions";
import { listingSellerClause } from "./listingSellerLabel";
import { formatShortPetId } from "./petDisplayUtils";

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
  const [listingsLoading, setListingsLoading] = useState(false);

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
    setListingsLoading(true);
    try {
      setListings(await getMarketListings(accessToken));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Couldn’t load pets for sale");
    } finally {
      setListingsLoading(false);
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
      setError("Choose a pet from your inventory before posting it for sale.");
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
      setError(e instanceof Error ? e.message : "Couldn’t post this pet for sale");
    }
  };

  const bid = async () => {
    setError(null);
    if (!selectedListing) {
      setError("Choose a pet for sale to bid on.");
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
    <section className="trading-pets-card trading-pets-secondary">
      <header className="trading-pets-card__header">
        <h2 id="trading-region-secondary-title">Resale marketplace</h2>
        <p className="muted small">
          <strong>Pets for sale</strong> are offers from you or other traders on the resale marketplace, each with an{" "}
          <strong>asking price</strong>. Buyers can pay the ask or place a <strong>bid</strong> for the seller to
          accept.
        </p>
      </header>
      <div className="trading-pets-card__body">
        <div className="trading-pets-form">
          <h3 className="trading-pets-subheading">Offer a pet for sale</h3>
          <label className="trading-pets-field">
            <span>Pet from your inventory</span>
            <select value={petId} onChange={(e) => setPetId(e.target.value)}>
              <option value="">Select…</option>
              {inventory.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.breedName} · {formatShortPetId(p.id)}
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
            Post for sale
          </button>
          {inventory.length === 0 ? (
            <p className="muted small">
              Add pets from primary supply, or open{" "}
              <Link to="/pets/my-pets" className="trading-pets-text-link">
                My pets
              </Link>{" "}
              to confirm your inventory — then choose a pet here to post for sale.
            </p>
          ) : null}
        </div>
        <div className="trading-pets-form">
          <h3 className="trading-pets-subheading">Pets for sale now</h3>
          <p className="muted small">Active offers from all traders (including yours).</p>
          {listingsLoading && listings.length === 0 ? (
            <p className="muted small trading-pets-panel-loading">Loading pets for sale…</p>
          ) : null}
          <ul className="trading-pets-list trading-pets-listings">
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
                  className={
                    isBidSelected
                      ? "trading-pets-listing-card trading-pets-listing-card--selected"
                      : "trading-pets-listing-card"
                  }
                >
                  <div className="trading-pets-listing-card__title">
                    <strong>{l.breedName}</strong>
                    <span className="muted small"> · Pet {formatShortPetId(l.petId)}</span>
                  </div>
                  <div className="trading-pets-listing-card__ask">
                    <span className="trading-pets-listing-card__ask-label">Asking price</span>
                    <span className="trading-pets-listing-card__ask-value">${l.askingPrice.toFixed(2)}</span>
                  </div>
                  {sellerClause ? <div className="muted small">{sellerClause}</div> : null}
                  <div className="muted small">
                    Recent trade (breed):{" "}
                    {l.recentTradePriceForBreed == null
                      ? "—"
                      : `$${l.recentTradePriceForBreed.toFixed(2)}`}{" "}
                    · New supply remaining: {l.remainingNewSupplyForBreed}
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
                          className="secondary-button trading-pets-listing-card__select"
                          onClick={() => {
                            setBidListingId(l.listingId);
                            setBidAmount(Math.max(1, Math.round(l.askingPrice * 100) / 100));
                          }}
                        >
                          Choose pet to bid on
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
              Use <strong>Choose pet to bid on</strong> on a card above, then set your amount here.
            </p>
          )}
          <p className="muted small">
            Bids at or above the ask settle immediately; lower bids lock cash until accepted, rejected, withdrawn, or
            replaced.
          </p>
        </div>
        {error ? <p className="trading-pets-error trading-pets-error--soft">{error}</p> : null}
      </div>
    </section>
  );
};
