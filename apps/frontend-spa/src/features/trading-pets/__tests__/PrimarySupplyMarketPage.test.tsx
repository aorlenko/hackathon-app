import { render, screen, waitFor, within } from "@testing-library/react";
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
  };
});

const mockUseMyPetTrader = vi.mocked(useMyPetTrader);
const getBreeds = vi.mocked(api.getBreeds);

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
    vi.mocked(api.getBreeds).mockResolvedValue([]);
    vi.mocked(api.getMarketListings).mockResolvedValue([]);
    vi.mocked(api.getNotifications).mockResolvedValue([]);
    mockUseMyPetTrader.mockReturnValue({
      traderId: baseSnapshot.traderId,
      snapshot: baseSnapshot,
      loading: false,
      error: null,
      refresh: vi.fn(),
      hubInvalidateSeq: 0,
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
});
