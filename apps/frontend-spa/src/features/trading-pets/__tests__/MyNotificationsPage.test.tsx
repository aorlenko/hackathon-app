import { render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { MyNotificationsPage } from "../MyNotificationsPage";
import { useMyPetTrader } from "../MyPetTraderContext";
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
    getNotifications: vi.fn(),
  };
});

const mockUseMyPetTrader = vi.mocked(useMyPetTrader);
const getNotifications = vi.mocked(api.getNotifications);

const renderPage = () =>
  render(
    <MemoryRouter>
      <MyNotificationsPage />
    </MemoryRouter>,
  );

describe("MyNotificationsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseMyPetTrader.mockReturnValue({
      traderId: "33333333-3333-3333-3333-000000000001",
      snapshot: null,
      loading: false,
      error: null,
      refresh: vi.fn(),
      hubInvalidateSeq: 0,
      showToast: vi.fn(),
    });
  });

  it("loads and shows notifications in newest-first table rows", async () => {
    getNotifications.mockResolvedValue([
      {
        id: "n1",
        type: "TradeCompleted",
        createdAt: "2026-01-02T12:00:00Z",
        petId: "p1",
        petName: "Milo",
        amount: 50,
        counterpartyTraderId: "t1",
        counterpartyDisplayName: "Alex",
      },
      {
        id: "n2",
        type: "BidReceived",
        createdAt: "2026-01-03T08:00:00Z",
        petId: "p2",
        petName: "Luna",
        amount: 10,
        counterpartyTraderId: "t2",
        counterpartyDisplayName: "Blake",
      },
    ]);

    renderPage();

    await waitFor(() =>
      expect(getNotifications).toHaveBeenCalledWith(
        "33333333-3333-3333-3333-000000000001",
        "test-token",
        250,
      ),
    );

    expect(screen.getByRole("heading", { name: /^My notifications$/i })).toBeInTheDocument();
    const rows = screen.getAllByRole("row");
    expect(rows).toHaveLength(3);
    const firstDataRow = rows[1];
    expect(within(firstDataRow).getByText("Bid received")).toBeInTheDocument();
    expect(within(firstDataRow).getByText("Luna")).toBeInTheDocument();
    expect(within(firstDataRow).getByText("$10.00")).toBeInTheDocument();
    expect(within(firstDataRow).getByText("Blake")).toBeInTheDocument();
  });

  it("shows empty copy when there are no notifications", async () => {
    getNotifications.mockResolvedValue([]);

    renderPage();

    await waitFor(() =>
      expect(screen.getByText(/no notifications yet/i)).toBeInTheDocument(),
    );
  });
});
