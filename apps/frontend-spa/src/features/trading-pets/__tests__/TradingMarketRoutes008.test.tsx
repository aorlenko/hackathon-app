import { render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Navigate, Route, Routes } from "react-router-dom";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { Header } from "../../../App";
import { MyPetTraderProvider } from "../MyPetTraderContext";
import { PrimarySupplyMarketPage } from "../PrimarySupplyMarketPage";
import { ResaleMarketplacePage } from "../ResaleMarketplacePage";
import * as api from "../tradingPetsApi";
import type { TraderSnapshotDto } from "../tradingPetsApi";

const { mockUseMyPetTrader } = vi.hoisted(() => ({
  mockUseMyPetTrader: vi.fn(),
}));

vi.mock("../../auth/AuthProvider", () => ({
  useTradingAuth: () => ({
    isAuthenticated: true,
    isLoading: false,
    accessToken: "test-token",
    displayName: "Demo",
    authError: null,
    mode: "demo" as const,
    login: vi.fn(),
    logout: vi.fn(),
    userId: "u1",
    accountSnapshot: null,
  }),
}));

vi.mock("../MyPetTraderContext", () => ({
  MyPetTraderProvider: () => {
    const { Outlet } = require("react-router-dom");
    return <Outlet />;
  },
  useMyPetTrader: () => mockUseMyPetTrader(),
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

const baseSnapshot: TraderSnapshotDto = {
  traderId: "33333333-3333-3333-3333-000000000001",
  displayName: "Demo",
  availableCash: 100,
  lockedCash: 10,
  portfolioTotal: 500,
  pets: [],
  myBids: [],
};

const renderTradingShell = (initialPath: string) =>
  render(
    <MemoryRouter initialEntries={[initialPath]}>
      <div>
        <Header />
        <main>
          <Routes>
            <Route element={<MyPetTraderProvider />}>
              <Route path="/pets/workspace" element={<Navigate to="/pets/primary-supply" replace />} />
              <Route path="/pets/primary-supply" element={<PrimarySupplyMarketPage />} />
              <Route path="/pets/resale" element={<ResaleMarketplacePage />} />
              <Route path="/pets/market" element={<Navigate to="/pets/resale" replace />} />
            </Route>
            <Route path="*" element={<Navigate to="/pets/primary-supply" replace />} />
          </Routes>
        </main>
      </div>
    </MemoryRouter>,
  );

describe("Trading market routes (008)", () => {
  beforeEach(() => {
    vi.mocked(api.getBreeds).mockResolvedValue([]);
    vi.mocked(api.getMarketListings).mockResolvedValue([]);
    mockUseMyPetTrader.mockReturnValue({
      traderId: baseSnapshot.traderId,
      snapshot: baseSnapshot,
      loading: false,
      error: null,
      refresh: vi.fn(),
      hubInvalidateSeq: 0,
    });
  });

  it("shows Primary supply market and Resale marketplace nav links", () => {
    renderTradingShell("/pets/primary-supply");
    const nav = screen.getByRole("navigation", { name: /^primary$/i });
    expect(within(nav).getByRole("link", { name: /^Primary supply market$/i })).toHaveAttribute(
      "href",
      "/pets/primary-supply",
    );
    expect(within(nav).getByRole("link", { name: /^Resale marketplace$/i })).toHaveAttribute("href", "/pets/resale");
  });

  it("redirects legacy /pets/workspace to primary supply with correct h1", async () => {
    renderTradingShell("/pets/workspace");
    await waitFor(() => expect(screen.getByRole("heading", { name: /^Primary supply market$/i })).toBeInTheDocument());
    expect(screen.queryByRole("heading", { name: /pet marketplace workspace/i })).not.toBeInTheDocument();
  });

  it("sends unknown paths to primary supply", async () => {
    renderTradingShell("/totally-unknown-route");
    await waitFor(() => expect(screen.getByRole("heading", { name: /^Primary supply market$/i })).toBeInTheDocument());
  });

  it("redirects legacy /pets/market to resale marketplace", async () => {
    renderTradingShell("/pets/market");
    await waitFor(() => expect(screen.getByRole("heading", { name: /^Resale marketplace$/i })).toBeInTheDocument());
  });
});
