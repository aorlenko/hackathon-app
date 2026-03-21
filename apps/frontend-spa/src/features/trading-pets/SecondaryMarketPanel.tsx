import { useCallback, useEffect, useMemo, useState } from "react";
import {
  getMarketListings,
  type MarketListingDto,
  type PetSummaryDto,
} from "./tradingPetsApi";
import { ResaleMarketListingRow } from "./ResaleMarketListingRow";
import { ResaleOfferPetForm } from "./ResaleOfferPetForm";
import { ResalePlaceBidPanel } from "./ResalePlaceBidPanel";

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

  useEffect(() => {
    if (selectedListing && selectedListing.sellerTraderId === traderId) {
      setBidListingId("");
    }
  }, [selectedListing, traderId]);

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

  const onSelectForBid = (l: MarketListingDto) => {
    if (l.sellerTraderId === traderId) {
      return;
    }
    setBidListingId(l.listingId);
    setBidAmount(Math.max(1, Math.round(l.askingPrice * 100) / 100));
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
        <ResaleOfferPetForm
          traderId={traderId}
          accessToken={accessToken}
          inventory={inventory}
          onListed={async () => {
            onChanged();
            await load();
          }}
        />
        <div className="trading-pets-form">
          <h3 className="trading-pets-subheading">Pets for sale now</h3>
          <p className="muted small">Active offers from all traders (including yours).</p>
          {listingsLoading && listings.length === 0 ? (
            <p className="muted small trading-pets-panel-loading">Loading pets for sale…</p>
          ) : null}
          <ul className="trading-pets-list trading-pets-listings">
            {listings.map((l) => (
              <ResaleMarketListingRow
                key={l.listingId}
                listing={l}
                traderId={traderId}
                accessToken={accessToken}
                bidListingId={bidListingId}
                allowBidSelection={l.sellerTraderId !== traderId}
                onSelectForBid={onSelectForBid}
                onSellerSideChanged={() => {
                  void load();
                  onChanged();
                }}
              />
            ))}
          </ul>
        </div>
        <ResalePlaceBidPanel
          traderId={traderId}
          accessToken={accessToken}
          selectedListing={selectedListing}
          onClearBidSelection={() => setBidListingId("")}
          bidAmount={bidAmount}
          onBidAmountChange={setBidAmount}
          onBidComplete={async () => {
            onChanged();
            await load();
          }}
          onBidError={setError}
        />
        {error ? <p className="trading-pets-error trading-pets-error--soft">{error}</p> : null}
      </div>
    </section>
  );
};
