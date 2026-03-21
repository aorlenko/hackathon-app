import { describe, expect, it } from "vitest";
import { partitionResaleListings } from "../marketListingsPartition";
import type { MarketListingDto } from "../tradingPetsApi";

const base = (overrides: Partial<MarketListingDto>): MarketListingDto => ({
  listingId: "L",
  petId: "p",
  sellerTraderId: "seller",
  breedName: "B",
  askingPrice: 1,
  sellerDisplayName: "S",
  sellerEmail: null,
  createdAt: "2026-01-01T00:00:00Z",
  recentTradePriceForBreed: null,
  remainingNewSupplyForBreed: 0,
  ...overrides,
});

describe("partitionResaleListings", () => {
  const me = "33333333-3333-3333-3333-000000000001";

  it("places own listings only in yourListings", () => {
    const mine = base({ listingId: "M1", sellerTraderId: me });
    const theirs = base({ listingId: "O1", sellerTraderId: "other" });
    const { yourListings, othersListings } = partitionResaleListings([mine, theirs], me);
    expect(yourListings).toEqual([mine]);
    expect(othersListings).toEqual([theirs]);
  });

  it("never puts sellerTraderId === traderId into othersListings", () => {
    const rows = [
      base({ listingId: "a", sellerTraderId: me }),
      base({ listingId: "b", sellerTraderId: "x" }),
      base({ listingId: "c", sellerTraderId: me }),
    ];
    const { othersListings } = partitionResaleListings(rows, me);
    expect(othersListings.every((l) => l.sellerTraderId !== me)).toBe(true);
  });

  it("covers all listings across both arrays", () => {
    const rows = [base({ listingId: "1" }), base({ listingId: "2", sellerTraderId: me })];
    const { yourListings, othersListings } = partitionResaleListings(rows, me);
    expect([...yourListings, ...othersListings].map((l) => l.listingId).sort()).toEqual(["1", "2"]);
  });
});
