import { useState } from "react";
import {
  acceptBid,
  rejectBid,
  withdrawListing,
} from "./tradingPetsApi";

type Props = {
  listingId: string;
  sellerTraderId: string;
  accessToken?: string;
  onChanged: () => void;
  /** When true, show accept/reject for the active below-ask bid. */
  hasPendingBid: boolean;
};

export const ListingSellerActions = ({
  listingId,
  sellerTraderId,
  accessToken,
  onChanged,
  hasPendingBid,
}: Props) => {
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const run = async (action: () => Promise<unknown>) => {
    setError(null);
    setBusy(true);
    try {
      await action();
      onChanged();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Action failed");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="trading-pets-seller-actions">
      {hasPendingBid ? (
        <>
          <button
            type="button"
            className="secondary-button"
            disabled={busy}
            onClick={() =>
              void run(() => acceptBid(listingId, { traderId: sellerTraderId }, accessToken))
            }
          >
            Accept a below-ask bid
          </button>
          <button
            type="button"
            className="secondary-button"
            disabled={busy}
            onClick={() =>
              void run(() => rejectBid(listingId, { traderId: sellerTraderId }, accessToken))
            }
          >
            Reject current bid
          </button>
        </>
      ) : null}
      <button
        type="button"
        className="secondary-button"
        disabled={busy}
        onClick={() =>
          void run(() =>
            withdrawListing(listingId, { traderId: sellerTraderId }, accessToken),
          )
        }
      >
        Remove from marketplace
      </button>
      {error ? <p className="trading-pets-error trading-pets-error--soft">{error}</p> : null}
    </div>
  );
};
