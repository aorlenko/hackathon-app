import { render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  TradingAuthContext,
  type TradingAuthState,
} from "../../auth/AuthProvider";
import { TradeHistoryPage } from "../TradeHistoryPage";
import * as settlementHistoryApi from "../settlementHistoryApi";
import * as tradeHistoryApi from "../tradeHistoryApi";
import { useResolvedAccountIdentities } from "../../account/useResolvedAccountIdentities";

vi.mock("../settlementHistoryApi", () => ({
  getUserSettlementHistory: vi.fn(),
}));

vi.mock("../tradeHistoryApi", () => ({
  getUserTradeHistory: vi.fn(),
}));

vi.mock("../../account/useResolvedAccountIdentities", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("../../account/useResolvedAccountIdentities")>();

  return {
    ...actual,
    useResolvedAccountIdentities: vi.fn(),
  };
});

const getUserSettlementHistory = vi.mocked(
  settlementHistoryApi.getUserSettlementHistory,
);
const getUserTradeHistory = vi.mocked(tradeHistoryApi.getUserTradeHistory);
const mockUseResolvedAccountIdentities = vi.mocked(useResolvedAccountIdentities);

const createAuthState = (
  overrides: Partial<TradingAuthState> = {},
): TradingAuthState => ({
  isAuthenticated: true,
  isLoading: false,
  userId: "user-1",
  displayName: "Current User",
  accessToken: "token",
  accountSnapshot: {
    userId: "user-1",
    displayName: "Current User",
    email: "current@example.com",
    cashAvailable: 1000,
  },
  authError: null,
  mode: "demo",
  login: vi.fn(async () => undefined),
  logout: vi.fn(),
  ...overrides,
});

const renderPage = (authState = createAuthState()) =>
  render(
    <MemoryRouter>
      <TradingAuthContext.Provider value={authState}>
        <TradeHistoryPage />
      </TradingAuthContext.Provider>
    </MemoryRouter>,
  );

describe("TradeHistoryPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseResolvedAccountIdentities.mockImplementation(() =>
      new Map([
        [
          "seller-1",
          {
            userId: "seller-1",
            displayName: "Seller Sam",
            email: "seller@example.com",
          },
        ],
        [
          "buyer-2",
          {
            userId: "buyer-2",
            displayName: "Buyer Blair",
            email: "buyer@example.com",
          },
        ],
      ]),
    );
  });

  it("shows side and counterparty instead of separate buyer and seller columns", async () => {
    getUserSettlementHistory.mockResolvedValue([
      {
        settlementId: "settlement-1",
        tradeId: "trade-buy",
        status: "SETTLED",
        startedAtUtc: "2026-03-20T10:05:00Z",
        completedAtUtc: "2026-03-20T10:10:00Z",
        failureReason: null,
      },
      {
        settlementId: "settlement-2",
        tradeId: "trade-sell",
        status: "IN_PROGRESS",
        startedAtUtc: "2026-03-21T11:05:00Z",
        completedAtUtc: null,
        failureReason: null,
      },
    ]);
    getUserTradeHistory.mockResolvedValue([
      {
        tradeId: "trade-buy",
        symbol: "CAT",
        price: 25,
        quantity: 2,
        executedAtUtc: "2026-03-20T10:00:00Z",
        buyerUserId: "user-1",
        sellerUserId: "seller-1",
      },
      {
        tradeId: "trade-sell",
        symbol: "DOG",
        price: 40,
        quantity: 1,
        executedAtUtc: "2026-03-21T11:00:00Z",
        buyerUserId: "buyer-2",
        sellerUserId: "user-1",
      },
    ]);

    renderPage();

    await waitFor(() => {
      expect(getUserSettlementHistory).toHaveBeenCalledWith("user-1", "token");
      expect(getUserTradeHistory).toHaveBeenCalledWith("user-1", "token");
    });

    expect(
      screen.getAllByRole("columnheader").map((header) => header.textContent),
    ).toEqual([
      "Side",
      "Price",
      "Qty",
      "Symbol",
      "With",
      "Trade time",
      "Settlement time",
    ]);
    expect(
      screen.queryByRole("columnheader", { name: "Settlement started" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("columnheader", { name: "Status" }),
    ).not.toBeInTheDocument();

    const buyRow = screen.getByText("CAT").closest("tr");
    const sellRow = screen.getByText("DOG").closest("tr");

    expect(buyRow).not.toBeNull();
    expect(sellRow).not.toBeNull();

    expect(within(buyRow!).getByText("Buy")).toBeInTheDocument();
    expect(within(buyRow!).getByText("Seller Sam")).toBeInTheDocument();

    expect(within(sellRow!).getByText("Sell")).toBeInTheDocument();
    expect(within(sellRow!).getByText("Buyer Blair")).toBeInTheDocument();
  });
});
