import { render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { ResaleMarketplacePage } from "../ResaleMarketplacePage";
import { useMyPetTrader } from "../MyPetTraderContext";
import type { MarketListingDto, TraderSnapshotDto } from "../tradingPetsApi";
import * as api from "../tradingPetsApi";

vi.mock("../../auth/AuthProvider", () => ({
  useTradingAuth: () => ({ accessToken: "test-token" }),
}));

vi.mock("../MyPetTraderContext", () => ({
  useMyPetTrader: vi.fn(),
}));

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

const mockUseMyPetTrader = vi.mocked(useMyPetTrader);
const getMarketListings = vi.mocked(api.getMarketListings);

const me = "33333333-3333-3333-3333-000000000001";

const baseSnapshot: TraderSnapshotDto = {
  traderId: me,
  displayName: "Demo",
  availableCash: 100,
  lockedCash: 0,
  portfolioTotal: 50,
  pets: [
    {
      id: "p-mine",
      breedName: "Mine Breed",
      ageYears: 1,
      health: 90,
      currentDesirability: 1,
      intrinsicValue: 10,
      isExpired: false,
      maintenanceCost: 1,
    },
  ],
  myBids: [],
};

const listingMine: MarketListingDto = {
  listingId: "L-mine",
  petId: "pet-mine",
  sellerTraderId: me,
  breedName: "Mine Breed",
  askingPrice: 50,
  sellerDisplayName: "Me",
  sellerEmail: null,
  createdAt: "2026-01-01T00:00:00Z",
  recentTradePriceForBreed: null,
  remainingNewSupplyForBreed: 3,
};

const listingOther: MarketListingDto = {
  listingId: "L-other",
  petId: "pet-other",
  sellerTraderId: "other-trader",
  breedName: "Their Breed",
  askingPrice: 80,
  sellerDisplayName: "Sam",
  sellerEmail: null,
  createdAt: "2026-01-02T00:00:00Z",
  recentTradePriceForBreed: 70,
  remainingNewSupplyForBreed: 2,
};

describe("ResaleMarketplacePage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseMyPetTrader.mockReturnValue({
      traderId: me,
      snapshot: baseSnapshot,
      loading: false,
      error: null,
      refresh: vi.fn(),
      hubInvalidateSeq: 0,
    });
    getMarketListings.mockResolvedValue([listingMine, listingOther]);
  });

  it("renders named regions for your listings and others offers", async () => {
    render(
      <MemoryRouter>
        <ResaleMarketplacePage />
      </MemoryRouter>,
    );
    await waitFor(() => expect(getMarketListings).toHaveBeenCalled());
    expect(screen.getByRole("heading", { name: /^Resale marketplace$/i })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: /your listings/i })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: /others' offers/i })).toBeInTheDocument();
  });

  it("keeps own listings out of the others column", async () => {
    render(
      <MemoryRouter>
        <ResaleMarketplacePage />
      </MemoryRouter>,
    );
    await waitFor(() => expect(getMarketListings).toHaveBeenCalled());
    const othersRegion = screen.getByRole("region", { name: /others' offers/i });
    expect(within(othersRegion).getByText(/Their Breed/)).toBeInTheDocument();
    expect(within(othersRegion).queryByText(/Mine Breed/)).not.toBeInTheDocument();
    const yoursRegion = screen.getByRole("region", { name: /your listings/i });
    expect(within(yoursRegion).getByText(/Mine Breed/)).toBeInTheDocument();
  });

  it("shows per-column empty messaging when partitioned lists are empty", async () => {
    getMarketListings.mockResolvedValue([]);
    render(
      <MemoryRouter>
        <ResaleMarketplacePage />
      </MemoryRouter>,
    );
    await waitFor(() => expect(getMarketListings).toHaveBeenCalled());
    expect(
      within(screen.getByRole("region", { name: /your listings/i })).getByText(/no active listings/i),
    ).toBeInTheDocument();
    expect(
      within(screen.getByRole("region", { name: /others' offers/i })).getByText(/no other traders have pets listed/i),
    ).toBeInTheDocument();
  });
});
