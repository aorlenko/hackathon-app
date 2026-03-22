import type { ReactNode } from "react";
import type { MarketListingDto } from "./tradingPetsApi";
import { ListingSellerActions } from "./ListingSellerActions";
import { listingSellerClause } from "./listingSellerLabel";
import { formatShortPetId } from "./petDisplayUtils";

export type MyActiveBidOnListing = {
  bidId: string;
  amount: number;
};

type Props = {
  listing: MarketListingDto;
  traderId: string;
  accessToken?: string;
  bidListingId: string;
  onSelectForBid: (listing: MarketListingDto) => void;
  onSellerSideChanged: () => void;
  /** When false (your listings column), bidding controls are hidden. */
  allowBidSelection: boolean;
  /** Your active below-ask bid on this listing (buyer view). */
  myActiveBid?: MyActiveBidOnListing | null;
  bidPanel?: ReactNode;
};

export const ResaleMarketListingRow = ({
  listing: l,
  traderId,
  accessToken,
  bidListingId,
  onSelectForBid,
  onSellerSideChanged,
  allowBidSelection,
  myActiveBid,
  bidPanel,
}: Props) => {
  const sellerClause = listingSellerClause(
    l.sellerTraderId,
    l.sellerDisplayName,
    l.sellerEmail,
    traderId,
  );
  const isBidSelected = l.listingId === bidListingId;
  const isOwn = l.sellerTraderId === traderId;

  return (
    <li
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
        {l.recentTradePriceForBreed == null ? "—" : `$${l.recentTradePriceForBreed.toFixed(2)}`} · New supply
        remaining: {l.remainingNewSupplyForBreed}
      </div>
      {isOwn ? (
        <>
          {l.activeBidAmount != null ? (
            <div className="trading-pets-active-bid" role="status" aria-live="polite">
              <span className="trading-pets-active-bid__label">Current below-ask bid</span>
              <span className="trading-pets-active-bid__amount">${l.activeBidAmount.toFixed(2)}</span>
              <span className="muted small trading-pets-active-bid__buyer">
                from {l.activeBidBuyerDisplayName?.trim() || "another trader"}
              </span>
            </div>
          ) : null}
          <ListingSellerActions
            listingId={l.listingId}
            sellerTraderId={traderId}
            accessToken={accessToken}
            onChanged={onSellerSideChanged}
            hasPendingBid={l.activeBidAmount != null}
          />
        </>
      ) : allowBidSelection ? (
        <div className="trading-pets-inline trading-pets-buyer-actions">
          {myActiveBid ? (
            <div className="trading-pets-your-bid" role="status" aria-live="polite">
              <span className="trading-pets-your-bid__label">Your pending bid</span>
              <span className="trading-pets-your-bid__amount">${myActiveBid.amount.toFixed(2)}</span>
              <span className="muted small trading-pets-your-bid__hint">
                Waiting for the seller. You can raise it from Place bid below.
              </span>
            </div>
          ) : null}
          {isBidSelected ? (
            <span className="muted small" aria-current="true">
              {myActiveBid ? (
                <>
                  Selected — enter a <strong>higher</strong> amount under Place bid, then submit.
                </>
              ) : (
                <>
                  Selected — use <strong>Place bid</strong> below
                </>
              )}
            </span>
          ) : (
            <button
              type="button"
              className="secondary-button trading-pets-listing-card__select"
              onClick={() => onSelectForBid(l)}
            >
              {myActiveBid ? "Raise your bid" : "Choose pet to bid on"}
            </button>
          )}
        </div>
      ) : null}
      {bidPanel}
    </li>
  );
};
