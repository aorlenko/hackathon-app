import type { MarketListingDto } from "./tradingPetsApi";
import { ListingSellerActions } from "./ListingSellerActions";
import { listingSellerClause } from "./listingSellerLabel";
import { formatShortPetId } from "./petDisplayUtils";

type Props = {
  listing: MarketListingDto;
  traderId: string;
  accessToken?: string;
  bidListingId: string;
  onSelectForBid: (listing: MarketListingDto) => void;
  onSellerSideChanged: () => void;
  /** When false (your listings column), bidding controls are hidden. */
  allowBidSelection: boolean;
};

export const ResaleMarketListingRow = ({
  listing: l,
  traderId,
  accessToken,
  bidListingId,
  onSelectForBid,
  onSellerSideChanged,
  allowBidSelection,
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
        <ListingSellerActions
          listingId={l.listingId}
          sellerTraderId={traderId}
          accessToken={accessToken}
          onChanged={onSellerSideChanged}
        />
      ) : allowBidSelection ? (
        <div className="trading-pets-inline">
          {isBidSelected ? (
            <span className="muted small" aria-current="true">
              Selected — use <strong>Place bid</strong> below
            </span>
          ) : (
            <button
              type="button"
              className="secondary-button trading-pets-listing-card__select"
              onClick={() => onSelectForBid(l)}
            >
              Choose pet to bid on
            </button>
          )}
        </div>
      ) : null}
    </li>
  );
};
