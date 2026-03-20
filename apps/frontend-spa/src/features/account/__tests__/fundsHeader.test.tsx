import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { vi } from "vitest";
import { Header } from "../../../App";
import {
  TradingAuthContext,
  type TradingAuthState,
} from "../../auth/AuthProvider";

const mockUseAccountFunds = vi.fn();

vi.mock("../useAccountFunds", () => ({
  useAccountFunds: (...args: unknown[]) => mockUseAccountFunds(...args),
}));

const createAuthState = (
  overrides: Partial<TradingAuthState> = {},
): TradingAuthState => ({
  isAuthenticated: true,
  isLoading: false,
  userId: "user-1",
  displayName: "Buyer One",
  accessToken: "token",
  accountSnapshot: null,
  mode: "demo",
  login: vi.fn(async () => undefined),
  logout: vi.fn(),
  ...overrides,
});

const renderHeader = (authState: TradingAuthState) =>
  render(
    <MemoryRouter>
      <TradingAuthContext.Provider value={authState}>
        <Header />
      </TradingAuthContext.Provider>
    </MemoryRouter>,
  );

describe("Header funds display", () => {
  it("shows a loading state while the initial funds snapshot is pending", () => {
    mockUseAccountFunds.mockReturnValue({
      formattedFunds: null,
      error: "",
      refresh: vi.fn(),
      snapshot: null,
      state: "loading",
    });

    renderHeader(createAuthState());

    expect(screen.getByText("Available funds")).toBeInTheDocument();
    expect(screen.getByText("Loading funds...")).toBeInTheDocument();
    expect(screen.getByText("loading")).toBeInTheDocument();
  });

  it("shows the confirmed funds amount in the persistent authenticated header", () => {
    mockUseAccountFunds.mockReturnValue({
      formattedFunds: "$250,000.00",
      error: "",
      refresh: vi.fn(),
      snapshot: {
        userId: "user-1",
        displayName: "Buyer One",
        email: "user1@example.com",
        cashAvailable: 250000,
        holdings: [{ symbol: "ABC", quantity: 10 }],
      },
      state: "confirmed",
    });

    renderHeader(createAuthState());

    expect(screen.getByText("Available funds")).toBeInTheDocument();
    expect(screen.getByText("$250,000.00")).toBeInTheDocument();
    expect(screen.getByText("confirmed")).toBeInTheDocument();
  });
});
