import type { MarketListingDto } from "./tradingPetsApi";

export type PartitionedResaleListings = {
  yourListings: MarketListingDto[];
  othersListings: MarketListingDto[];
};

/**
 * Splits open market listings for resale UI: yours vs everyone else (FR-004).
 * Mutually exclusive buckets for a given `traderId`.
 */
export function partitionResaleListings(
  listings: MarketListingDto[],
  traderId: string,
): PartitionedResaleListings {
  const yourListings: MarketListingDto[] = [];
  const othersListings: MarketListingDto[] = [];
  for (const listing of listings) {
    if (listing.sellerTraderId === traderId) {
      yourListings.push(listing);
    } else {
      othersListings.push(listing);
    }
  }
  return { yourListings, othersListings };
}
