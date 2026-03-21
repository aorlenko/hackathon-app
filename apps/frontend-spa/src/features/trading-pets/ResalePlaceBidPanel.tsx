import type { MarketListingDto } from "./tradingPetsApi";
import { placeBid } from "./tradingPetsApi";

type Props = {
  traderId: string;
  accessToken?: string;
  selectedListing: MarketListingDto | null;
  /** Your active below-ask bid on the selected listing, if any. */
  selectedListingPendingBidAmount?: number | null;
  onClearBidSelection: () => void;
  bidAmount: number;
  onBidAmountChange: (n: number) => void;
  onBidComplete: () => void | Promise<void>;
  onBidError: (message: string | null) => void;
  /**
   * Inline under Others&apos; offers: no empty-state copy; render nothing until a listing is selected.
   * @default false
   */
  embedded?: boolean;
};

export const ResalePlaceBidPanel = ({
  traderId,
  accessToken,
  selectedListing,
  selectedListingPendingBidAmount = null,
  onClearBidSelection,
  bidAmount,
  onBidAmountChange,
  onBidComplete,
  onBidError,
  embedded = false,
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
    const pending = selectedListingPendingBidAmount;
    if (
      pending != null &&
      bidAmount < selectedListing.askingPrice &&
      bidAmount <= pending
    ) {
      onBidError(
        `Enter more than your current bid ($${pending.toFixed(2)}) or match the asking price to buy now.`,
      );
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

  if (embedded && !othersListing) {
    return null;
  }

  const formClass =
    embedded ? "trading-pets-form trading-pets-form--embedded-bid" : "trading-pets-form";

  return (
    <div className={formClass}>
      <h3
        id={embedded ? "resale-inline-bid-heading" : undefined}
        className={
          embedded ? "trading-pets-subheading trading-pets-subheading--embedded-bid" : "trading-pets-subheading"
        }
      >
        Place bid
      </h3>
      {othersListing ? (
        <>
          <div className="trading-pets-bid-target" aria-live="polite">
            <div className="trading-pets-bid-target__row">
              <span className="trading-pets-bid-target__label">You&apos;re bidding on</span>
              <button
                type="button"
                className="inline-link trading-pets-bid-target__cancel"
                onClick={() => onClearBidSelection()}
              >
                Cancel
              </button>
            </div>
            <p className="trading-pets-bid-target__summary">
              <strong>{othersListing.breedName}</strong>
              <span className="muted"> · asking </span>
              ${othersListing.askingPrice.toFixed(2)}
            </p>
          </div>
          {selectedListingPendingBidAmount != null ? (
            <p className="muted small">
              Your current pending bid on this listing is{" "}
              <strong>${selectedListingPendingBidAmount.toFixed(2)}</strong>. To replace it, enter a higher amount
              below (or the asking price for an instant purchase).
            </p>
          ) : null}
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
            {selectedListingPendingBidAmount != null ? "Raise bid" : "Submit bid"}
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
