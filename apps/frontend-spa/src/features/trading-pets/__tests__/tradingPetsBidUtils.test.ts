import { describe, expect, it } from "vitest";
import { findActiveMyBidForListing, minRaiseBidAmount } from "../tradingPetsBidUtils";

describe("tradingPetsBidUtils", () => {
  it("findActiveMyBidForListing returns active bid for listing", () => {
    const b = findActiveMyBidForListing(
      [
        {
          bidId: "b1",
          listingId: "L1",
          petId: "p1",
          amount: 10,
          status: "Active",
        },
      ],
      "L1",
    );
    expect(b?.amount).toBe(10);
  });

  it("findActiveMyBidForListing ignores non-active", () => {
    expect(
      findActiveMyBidForListing(
        [{ bidId: "b1", listingId: "L1", petId: "p1", amount: 10, status: "Rejected" }],
        "L1",
      ),
    ).toBeUndefined();
  });

  it("minRaiseBidAmount steps by one cent", () => {
    expect(minRaiseBidAmount(40)).toBe(40.01);
    expect(minRaiseBidAmount(40.5)).toBe(40.51);
  });
});
