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
};

export const ListingSellerActions = ({
  listingId,
  sellerTraderId,
  accessToken,
  onChanged,
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
      <button
        type="button"
        className="secondary-button"
        disabled={busy}
        onClick={() =>
          void run(() => acceptBid(listingId, { traderId: sellerTraderId }, accessToken))
        }
      >
        Accept below-ask bid
      </button>
      <button
        type="button"
        className="secondary-button"
        disabled={busy}
        onClick={() =>
          void run(() => rejectBid(listingId, { traderId: sellerTraderId }, accessToken))
        }
      >
        Reject bid
      </button>
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
        Withdraw listing
      </button>
      {error ? <p className="trading-pets-error">{error}</p> : null}
    </div>
  );
};
