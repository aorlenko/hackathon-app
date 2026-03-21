import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { MyPetsPage } from "../MyPetsPage";
import { useMyPetTrader } from "../MyPetTraderContext";
import type { TraderSnapshotDto } from "../tradingPetsApi";

vi.mock("../MyPetTraderContext", () => ({
  useMyPetTrader: vi.fn(),
}));

const mockUseMyPetTrader = vi.mocked(useMyPetTrader);

const snapshot: TraderSnapshotDto = {
  traderId: "33333333-3333-3333-3333-000000000001",
  displayName: "Demo",
  availableCash: 1,
  lockedCash: 0,
  portfolioTotal: 2,
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

describe("MyPetsPage", () => {
  beforeEach(() => {
    mockUseMyPetTrader.mockReturnValue({
      traderId: snapshot.traderId,
      snapshot,
      loading: false,
      error: null,
      refresh: vi.fn(),
      hubInvalidateSeq: 0,
    });
  });

  it("lists one row per pet with short id and attributes", () => {
    render(
      <MemoryRouter>
        <MyPetsPage />
      </MemoryRouter>,
    );
    const region = screen.getByRole("region", { name: /your inventory/i });
    expect(within(region).getByText(/Test Breed/)).toBeInTheDocument();
    expect(within(region).getByText(/Pet ID 00000099/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /offer for resale/i })).not.toBeInTheDocument();
    expect(within(region).queryByRole("img")).not.toBeInTheDocument();
  });

  it("shows breed thumbnail when breedImageUrl is provided", () => {
    mockUseMyPetTrader.mockReturnValue({
      traderId: snapshot.traderId,
      snapshot: {
        ...snapshot,
        pets: [
          {
            ...snapshot.pets[0],
            breedName: "Beagle",
            breedImageUrl: "https://example.com/beagle.jpg",
          },
        ],
      },
      loading: false,
      error: null,
      refresh: vi.fn(),
      hubInvalidateSeq: 0,
    });
    render(
      <MemoryRouter>
        <MyPetsPage />
      </MemoryRouter>,
    );
    const img = screen.getByRole("img", { name: /beagle \(breed reference\)/i });
    expect(img).toHaveAttribute("src", "https://example.com/beagle.jpg");
  });
});
