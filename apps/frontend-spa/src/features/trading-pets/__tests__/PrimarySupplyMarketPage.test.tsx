import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { PrimarySupplyMarketPage } from "../PrimarySupplyMarketPage";
import { useMyPetTrader } from "../MyPetTraderContext";
import type { TraderSnapshotDto } from "../tradingPetsApi";
import * as api from "../tradingPetsApi";

vi.mock("../../auth/AuthProvider", () => ({
  useTradingAuth: () => ({ accessToken: "test-token" }),
}));

vi.mock("../MyPetTraderContext", () => ({
  useMyPetTrader: vi.fn(),
}));

vi.mock("../tradingPetsApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../tradingPetsApi")>();
  return {
    ...actual,
    getBreeds: vi.fn().mockResolvedValue([]),
    getMarketListings: vi.fn().mockResolvedValue([]),
    getNotifications: vi.fn().mockResolvedValue([]),
    purchasePets: vi.fn(),
  };
});

const mockUseMyPetTrader = vi.mocked(useMyPetTrader);
const getBreeds = vi.mocked(api.getBreeds);
const purchasePets = vi.mocked(api.purchasePets);
const refreshMock = vi.fn();
const showToastMock = vi.fn();

const baseSnapshot: TraderSnapshotDto = {
  traderId: "33333333-3333-3333-3333-000000000001",
  displayName: "Demo",
  availableCash: 100,
  lockedCash: 10,
  portfolioTotal: 500,
  pets: [
    {
      id: "aaaaaaaa-aaaa-aaaa-aaaa-000000000099",
      breedName: "Test Breed",
      ageYears: 1.5,
      health: 88,
      currentDesirability: 12,
      intrinsicValue: 40,
      isExpired: false,
      maintenanceCost: 2,
    },
  ],
  myBids: [],
};

const renderPage = () =>
  render(
    <MemoryRouter>
      <PrimarySupplyMarketPage />
    </MemoryRouter>,
  );

describe("PrimarySupplyMarketPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(api.getBreeds).mockResolvedValue([]);
    vi.mocked(api.getMarketListings).mockResolvedValue([]);
    vi.mocked(api.getNotifications).mockResolvedValue([]);
    purchasePets.mockResolvedValue({ pets: [], availableCash: 75 });
    mockUseMyPetTrader.mockReturnValue({
      traderId: baseSnapshot.traderId,
      snapshot: baseSnapshot,
      loading: false,
      error: null,
      refresh: refreshMock,
      hubInvalidateSeq: 0,
      showToast: showToastMock,
    });
  });

  it("mentions pet trader strip and links to My pets and resale", async () => {
    renderPage();
    await waitFor(() => expect(getBreeds).toHaveBeenCalled());
    expect(screen.getByText(/header box next to your account/i)).toBeInTheDocument();
    const myPets = screen.getByRole("link", { name: /^My pets$/i });
    expect(myPets).toHaveAttribute("href", "/pets/my-pets");
    expect(screen.getByRole("link", { name: /^Resale marketplace$/i })).toHaveAttribute("href", "/pets/resale");
  });

  it("exposes primary supply region with PrimaryMarketPanel", async () => {
    renderPage();
    await waitFor(() => expect(getBreeds).toHaveBeenCalled());
    expect(screen.getByRole("heading", { name: /^Primary supply market$/i })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: /primary supply/i })).toBeInTheDocument();
    expect(screen.queryByRole("region", { name: /your listings/i })).not.toBeInTheDocument();
  });

  it("uses marketplace copy in the page intro, not generic exchange framing", async () => {
    renderPage();
    await waitFor(() => expect(getBreeds).toHaveBeenCalled());
    const title = screen.getByRole("heading", { name: /^Primary supply market$/i });
    const headerEl = title.closest("header");
    expect(headerEl).toBeTruthy();
    expect(
      within(headerEl as HTMLElement).getByText(/buy new pets from primary supply at each breed/i),
    ).toBeInTheDocument();
    expect(screen.queryByText(/exchange terminal/i)).not.toBeInTheDocument();
  });

  it("shows a popup toast and refreshes balances after a successful primary purchase", async () => {
    getBreeds.mockResolvedValue([
      {
        id: "breed-1",
        name: "Aurora Cat",
        category: "cats",
        lifespanYears: 10,
        baselineDesirability: 12,
        maintenanceCost: 3,
        retailPrice: 25.5,
        remainingSupply: 4,
      },
    ]);
    renderPage();
    await waitFor(() => expect(getBreeds).toHaveBeenCalled());

    fireEvent.click(screen.getByRole("button", { name: /buy from primary supply/i }));

    await waitFor(() =>
      expect(purchasePets).toHaveBeenCalledWith(
        {
          traderId: baseSnapshot.traderId,
          breedId: "breed-1",
          quantity: 1,
        },
        "test-token",
      ),
    );
    expect(showToastMock).toHaveBeenCalledWith(
      expect.objectContaining({
        title: "Primary purchase completed",
        body: "1 pet · Aurora Cat · $25.50",
        variant: "trade",
      }),
    );
    expect(showToastMock.mock.calls[0]?.[0]?.dedupeKey).toMatch(
      /^primary-purchase-33333333-3333-3333-3333-000000000001-/,
    );
    expect(refreshMock).toHaveBeenCalled();
  });
});
