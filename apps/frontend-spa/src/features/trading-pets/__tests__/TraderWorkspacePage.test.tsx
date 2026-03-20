import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  TradingAuthContext,
  type TradingAuthState,
} from "../../auth/AuthProvider";
import { MyPetTraderProvider } from "../MyPetTraderContext";
import { TraderWorkspacePage } from "../TraderWorkspacePage";
import type { TerminalMarketRowDto } from "../tradingPetsApi";

const mocks = vi.hoisted(() => ({
  getMyTraderSnapshot: vi.fn(),
  getTerminalMarkets: vi.fn(),
  getTerminalWorkspace: vi.fn(),
}));

vi.mock("../useTradingPetsRealtime", () => ({
  useTradingPetsRealtime: () => {},
}));

vi.mock("../tradingPetsApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../tradingPetsApi")>();
  return {
    ...actual,
    getMyTraderSnapshot: mocks.getMyTraderSnapshot,
    getTerminalMarkets: mocks.getTerminalMarkets,
    getTerminalWorkspace: mocks.getTerminalWorkspace,
  };
});

const mkMarket = (id: string, name: string): TerminalMarketRowDto => ({
  marketEntryId: id,
  displayName: name,
  currentSupply: 2,
  latestTradePrice: 10,
  bestBidPrice: 9,
  bestAskPrice: 11,
  trendDirection: "Up",
  lastTradeAt: new Date().toISOString(),
});

const mkWorkspace = (id: string, name: string) => ({
  marketEntry: mkMarket(id, name),
  orderBook: {
    capturedAt: new Date().toISOString(),
    bids: [{ price: 9, quantity: 2, orderCount: 1 }],
    asks: [] as { price: number; quantity: number; orderCount: number }[],
  },
  accountSummary: {
    displayName: "Demo",
    availableCash: 100,
    lockedCash: 0,
    portfolioTotal: 100,
    ownedQuantity: 1,
    eligibleAskQuantity: 1,
  },
  recentTrades: [] as {
    tradeId: string;
    price: number;
    quantity: number;
    executedAt: string;
    executionType: string;
  }[],
  lastUpdatedAt: new Date().toISOString(),
});

const authState: TradingAuthState = {
  isAuthenticated: true,
  isLoading: false,
  userId: "user-1",
  displayName: "Demo",
  accessToken: "test-token",
  accountSnapshot: null,
  authError: null,
  mode: "demo",
  login: vi.fn(async () => undefined),
  logout: vi.fn(),
};

const renderWorkspace = () =>
  render(
    <MemoryRouter initialEntries={["/pets/workspace"]}>
      <TradingAuthContext.Provider value={authState}>
        <Routes>
          <Route element={<MyPetTraderProvider />}>
            <Route path="/pets/workspace" element={<TraderWorkspacePage />} />
          </Route>
        </Routes>
      </TradingAuthContext.Provider>
    </MemoryRouter>,
  );

describe("TraderWorkspacePage terminal", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getMyTraderSnapshot.mockResolvedValue({
      traderId: "11111111-1111-1111-1111-111111111111",
      displayName: "Demo",
      availableCash: 100,
      lockedCash: 0,
      portfolioTotal: 100,
      pets: [],
      myBids: [],
    });
    mocks.getTerminalMarkets.mockResolvedValue([
      mkMarket("m1", "Breed A"),
      mkMarket("m2", "Breed B"),
    ]);
    mocks.getTerminalWorkspace.mockImplementation(async (id: string) =>
      mkWorkspace(id, id === "m1" ? "Breed A" : "Breed B"),
    );
  });

  it("renders the terminal route and loads the default market workspace", async () => {
    renderWorkspace();

    expect(
      await screen.findByRole("heading", { name: "Trading terminal" }),
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(mocks.getTerminalMarkets).toHaveBeenCalledWith("test-token");
    });
    await waitFor(() => {
      expect(mocks.getTerminalWorkspace).toHaveBeenCalledWith(
        "m1",
        "test-token",
      );
    });

    const book = await screen.findByRole("region", { name: "Order book" });
    expect(book).toHaveTextContent("Breed A");
  });

  it("loads another market when the user selects it", async () => {
    const user = userEvent.setup();
    renderWorkspace();

    await screen.findByRole("heading", { name: "Trading terminal" });

    await waitFor(() => {
      expect(mocks.getTerminalWorkspace).toHaveBeenCalledWith(
        "m1",
        "test-token",
      );
    });

    await user.click(screen.getByRole("button", { name: /Breed B/i }));

    await waitFor(() => {
      expect(mocks.getTerminalWorkspace).toHaveBeenCalledWith(
        "m2",
        "test-token",
      );
    });

    const book = await screen.findByRole("region", { name: "Order book" });
    await waitFor(() => {
      expect(book).toHaveTextContent("Breed B");
    });
  });
});
