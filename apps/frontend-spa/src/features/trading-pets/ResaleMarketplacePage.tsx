import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useTradingAuth } from "../auth/AuthProvider";
import { useMyPetTrader } from "./MyPetTraderContext";
import { getMarketListings, type MarketListingDto } from "./tradingPetsApi";
import { partitionResaleListings } from "./marketListingsPartition";
import { ResaleMarketListingRow } from "./ResaleMarketListingRow";
import { ResaleOfferPetForm } from "./ResaleOfferPetForm";
import { ResalePlaceBidPanel } from "./ResalePlaceBidPanel";

export const ResaleMarketplacePage = () => {
  const auth = useTradingAuth();
  const { traderId, snapshot, error: traderError, refresh, hubInvalidateSeq } = useMyPetTrader();
  const [listings, setListings] = useState<MarketListingDto[]>([]);
  const [bidListingId, setBidListingId] = useState("");
  const [bidAmount, setBidAmount] = useState(25);
  const [listingsLoading, setListingsLoading] = useState(false);
  const [listingsError, setListingsError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const { yourListings, othersListings } = useMemo(
    () => partitionResaleListings(listings, traderId),
    [listings, traderId],
  );

  const selectedListing = useMemo(
    () => listings.find((l) => l.listingId === bidListingId) ?? null,
    [listings, bidListingId],
  );

  const load = useCallback(async () => {
    setListingsError(null);
    setListingsLoading(true);
    try {
      setListings(await getMarketListings(auth.accessToken));
    } catch (e) {
      setListingsError(e instanceof Error ? e.message : "Couldn’t load pets for sale");
    } finally {
      setListingsLoading(false);
    }
  }, [auth.accessToken]);

  useEffect(() => {
    if (!auth.accessToken || !traderId) {
      return;
    }
    void load();
  }, [auth.accessToken, traderId, hubInvalidateSeq, load]);

  useEffect(() => {
    if (bidListingId && !listings.some((l) => l.listingId === bidListingId)) {
      setBidListingId("");
    }
  }, [listings, bidListingId]);

  useEffect(() => {
    if (selectedListing && selectedListing.sellerTraderId === traderId) {
      setBidListingId("");
    }
  }, [selectedListing, traderId]);

  const onSelectForBid = (l: MarketListingDto) => {
    if (l.sellerTraderId === traderId) {
      return;
    }
    setActionError(null);
    setBidListingId(l.listingId);
    setBidAmount(Math.max(1, Math.round(l.askingPrice * 100) / 100));
  };

  const afterListingMutation = async () => {
    void refresh();
    await load();
  };

  return (
    <div className="trading-pets-workspace trading-pets-page--resale">
      <header className="trading-pets-page__header">
        <h1>Resale marketplace</h1>
        <p className="muted trading-pets-page__intro trading-pets-page__intro--full">
          Offer pets from your inventory, track your active listings, and bid on other traders&apos; asks. New supply and
          retail purchases stay on{" "}
          <Link to="/pets/primary-supply" className="trading-pets-text-link">
            Primary supply market
          </Link>
          .
        </p>
      </header>

      {traderError && snapshot ? (
        <p className="trading-pets-workspace__banner" role="alert">
          Could not refresh your latest balances and inventory ({traderError}). Figures in the header summary are from
          your last successful load.{" "}
          <button type="button" className="trading-pets-inline-action" onClick={() => void refresh()}>
            Try again
          </button>
        </p>
      ) : null}

      <section className="trading-pets-card trading-pets-resale__offer" aria-labelledby="resale-offer-title">
        <h2 id="resale-offer-title" className="visually-hidden">
          Offer a pet for sale
        </h2>
        <div className="trading-pets-card__body">
          <ResaleOfferPetForm
            traderId={traderId}
            accessToken={auth.accessToken}
            inventory={snapshot?.pets ?? []}
            onListed={afterListingMutation}
          />
        </div>
      </section>

      <div className="trading-pets-resale-columns">
        <section
          className="trading-pets-card trading-pets-resale-column"
          role="region"
          aria-labelledby="resale-your-listings-title"
        >
          <header className="trading-pets-card__header">
            <h2 id="resale-your-listings-title">Your listings</h2>
            <p className="muted small">Active resale offers you have posted.</p>
          </header>
          <div className="trading-pets-card__body">
            {listingsError ? (
              <p className="trading-pets-error trading-pets-error--soft" role="alert">
                {listingsError}{" "}
                <button type="button" className="trading-pets-inline-action" onClick={() => void load()}>
                  Try again
                </button>
              </p>
            ) : null}
            {listingsLoading && yourListings.length === 0 && !listingsError ? (
              <p className="muted small trading-pets-panel-loading">Loading your listings…</p>
            ) : null}
            {!listingsLoading && !listingsError && yourListings.length === 0 ? (
              <p className="trading-pets-empty trading-pets-empty--column">
                You have no active listings. Post a pet above, or check back after you buy inventory on primary supply.
              </p>
            ) : null}
            <ul className="trading-pets-list trading-pets-listings">
              {yourListings.map((l) => (
                <ResaleMarketListingRow
                  key={l.listingId}
                  listing={l}
                  traderId={traderId}
                  accessToken={auth.accessToken}
                  bidListingId={bidListingId}
                  allowBidSelection={false}
                  onSelectForBid={onSelectForBid}
                  onSellerSideChanged={() => void afterListingMutation()}
                />
              ))}
            </ul>
          </div>
        </section>

        <section
          className="trading-pets-card trading-pets-resale-column"
          role="region"
          aria-labelledby="resale-others-offers-title"
        >
          <header className="trading-pets-card__header">
            <h2 id="resale-others-offers-title">Others&apos; offers</h2>
            <p className="muted small">Listings from other traders you can bid on.</p>
          </header>
          <div className="trading-pets-card__body">
            {listingsError ? (
              <p className="trading-pets-error trading-pets-error--soft" role="alert">
                {listingsError}{" "}
                <button type="button" className="trading-pets-inline-action" onClick={() => void load()}>
                  Try again
                </button>
              </p>
            ) : null}
            {listingsLoading && othersListings.length === 0 && !listingsError ? (
              <p className="muted small trading-pets-panel-loading">Loading others&apos; offers…</p>
            ) : null}
            {!listingsLoading && !listingsError && othersListings.length === 0 ? (
              <p className="trading-pets-empty trading-pets-empty--column">
                No other traders have pets listed right now. Try again later or widen your search from{" "}
                <Link to="/pets/market" className="trading-pets-text-link">
                  Pets for sale
                </Link>
                .
              </p>
            ) : null}
            <ul className="trading-pets-list trading-pets-listings">
              {othersListings.map((l) => (
                <ResaleMarketListingRow
                  key={l.listingId}
                  listing={l}
                  traderId={traderId}
                  accessToken={auth.accessToken}
                  bidListingId={bidListingId}
                  allowBidSelection
                  onSelectForBid={onSelectForBid}
                  onSellerSideChanged={() => void afterListingMutation()}
                />
              ))}
            </ul>
          </div>
        </section>
      </div>

      <section className="trading-pets-card trading-pets-resale__bid" aria-labelledby="resale-bid-title">
        <h2 id="resale-bid-title" className="visually-hidden">
          Place bid
        </h2>
        <div className="trading-pets-card__body">
          <ResalePlaceBidPanel
            traderId={traderId}
            accessToken={auth.accessToken}
            selectedListing={selectedListing}
            onClearBidSelection={() => {
              setActionError(null);
              setBidListingId("");
            }}
            bidAmount={bidAmount}
            onBidAmountChange={setBidAmount}
            onBidComplete={afterListingMutation}
            onBidError={setActionError}
          />
          {actionError ? <p className="trading-pets-error trading-pets-error--soft">{actionError}</p> : null}
        </div>
      </section>
    </div>
  );
};
