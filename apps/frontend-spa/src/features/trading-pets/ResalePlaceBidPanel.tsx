import type { MarketListingDto } from "./tradingPetsApi";
import { placeBid } from "./tradingPetsApi";

type Props = {
  traderId: string;
  accessToken?: string;
  selectedListing: MarketListingDto | null;
  onClearBidSelection: () => void;
  bidAmount: number;
  onBidAmountChange: (n: number) => void;
  onBidComplete: () => void | Promise<void>;
  onBidError: (message: string | null) => void;
};

export const ResalePlaceBidPanel = ({
  traderId,
  accessToken,
  selectedListing,
  onClearBidSelection,
  bidAmount,
  onBidAmountChange,
  onBidComplete,
  onBidError,
}: Props) => {
  const bid = async () => {
    onBidError(null);
    if (!selectedListing) {
      onBidError("Choose a pet for sale to bid on.");
      return;
    }
    if (selectedListing.sellerTraderId === traderId) {
      onBidError("Bids apply to other traders’ listings — pick an offer under Others’ offers.");
      return;
    }
    try {
      await placeBid(selectedListing.listingId, { traderId, amount: bidAmount }, accessToken);
      onClearBidSelection();
      await onBidComplete();
    } catch (e) {
      onBidError(e instanceof Error ? e.message : "Could not place bid");
    }
  };

  const othersListing =
    selectedListing && selectedListing.sellerTraderId !== traderId ? selectedListing : null;

  return (
    <div className="trading-pets-form">
      <h3 className="trading-pets-subheading">Place bid</h3>
      {othersListing ? (
        <>
          <div className="trading-pets-bid-target" aria-live="polite">
            <div className="trading-pets-bid-target__row">
              <span className="trading-pets-bid-target__label">You&apos;re bidding on</span>
              <button
                type="button"
                className="inline-link trading-pets-bid-target__change"
                onClick={() => onClearBidSelection()}
              >
                Change
              </button>
            </div>
            <p className="trading-pets-bid-target__summary">
              <strong>{othersListing.breedName}</strong>
              <span className="muted"> · asking </span>
              ${othersListing.askingPrice.toFixed(2)}
            </p>
          </div>
          <label className="trading-pets-field">
            <span>Your bid amount</span>
            <input
              type="number"
              min={1}
              step={0.01}
              value={bidAmount}
              onChange={(e) => onBidAmountChange(Number(e.target.value) || 1)}
            />
          </label>
          <button type="button" className="primary-button" onClick={() => void bid()}>
            Submit bid
          </button>
        </>
      ) : (
        <p className="muted small">
          Use <strong>Choose pet to bid on</strong> under <strong>Others&apos; offers</strong>, then set your amount
          here.
        </p>
      )}
      <p className="muted small">
        Bids at or above the ask settle immediately; lower bids lock cash until accepted, rejected, withdrawn, or
        replaced.
      </p>
    </div>
  );
};
