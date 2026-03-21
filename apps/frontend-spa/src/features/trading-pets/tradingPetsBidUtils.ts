import type { TraderSnapshotDto } from "./tradingPetsApi";

export function findActiveMyBidForListing(
  myBids: TraderSnapshotDto["myBids"] | undefined,
  listingId: string,
) {
  return myBids?.find((b) => b.listingId === listingId && b.status === "Active");
}

/** Next valid bid strictly above `currentBidAmount` (server rejects ties). */
export function minRaiseBidAmount(currentBidAmount: number): number {
  return Math.max(0.01, Math.floor(currentBidAmount * 100 + 1) / 100);
}
