import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { vi } from "vitest";
import { Header } from "../../../App";
import {
  TradingAuthContext,
  type TradingAuthState,
} from "../../auth/AuthProvider";

const createAuthState = (
  overrides: Partial<TradingAuthState> = {},
): TradingAuthState => ({
  isAuthenticated: true,
  isLoading: false,
  userId: "user-1",
  displayName: "Buyer One",
  accessToken: "token",
  accountSnapshot: null,
  authError: null,
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

describe("Header authenticated panel", () => {
  it("shows identity and sign-out; account FundsPanel was removed from the shell", () => {
    renderHeader(createAuthState());

    expect(screen.getByText("Buyer One")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /demo sign-in/i })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign out/i })).toBeInTheDocument();
    expect(screen.queryByText("Available funds")).not.toBeInTheDocument();
  });
});
