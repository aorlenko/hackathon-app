import { render, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { SecondaryMarketPanel } from "../SecondaryMarketPanel";
import * as api from "../tradingPetsApi";

vi.mock("../ListingSellerActions", () => ({
  ListingSellerActions: () => <div data-testid="seller-actions" />,
}));

vi.mock("../tradingPetsApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../tradingPetsApi")>();
  return {
    ...actual,
    getMarketListings: vi.fn(),
    createListing: vi.fn(),
    placeBid: vi.fn(),
  };
});

const getMarketListings = vi.mocked(api.getMarketListings);

describe("SecondaryMarketPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getMarketListings.mockResolvedValue([
      {
        listingId: "L1",
        petId: "bbbbbbbb-bbbb-bbbb-bbbb-000000000088",
        sellerTraderId: "other",
        breedName: "Moon Dog",
        askingPrice: 120,
        sellerDisplayName: "Seller Sam",
        sellerEmail: null,
        createdAt: "2026-01-01T00:00:00Z",
        recentTradePriceForBreed: 100,
        remainingNewSupplyForBreed: 5,
      },
    ]);
  });

  it("shows pet identity, asking price, seller context, and action controls", async () => {
    render(
      <SecondaryMarketPanel
        traderId="self"
        accessToken="tok"
        reloadToken={0}
        inventory={[
          {
            id: "p1",
            breedName: "Moon Dog",
            ageYears: 2,
            health: 90,
            currentDesirability: 5,
            intrinsicValue: 80,
            isExpired: false,
            maintenanceCost: 3,
          },
        ]}
        onChanged={vi.fn()}
      />,
    );

    await waitFor(() => expect(getMarketListings).toHaveBeenCalled());

    const listingCard = screen.getAllByRole("listitem")[0];
    expect(within(listingCard).getByText(/Moon Dog/)).toBeInTheDocument();
    expect(within(listingCard).getByText(/Asking price/i)).toBeInTheDocument();
    expect(within(listingCard).getByText("$120.00")).toBeInTheDocument();
    expect(within(listingCard).getByText(/Seller:\s*Seller Sam/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /choose pet to bid on/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /post for sale/i })).toBeInTheDocument();
  });
});
