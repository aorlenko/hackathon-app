import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { TerminalTradingPanel } from "../terminal/TerminalTradingPanel";
import type {
  TerminalAccountSummaryDto,
  TerminalMarketRowDto,
} from "../tradingPetsApi";

const mocks = vi.hoisted(() => ({
  submitTerminalBid: vi.fn(),
  submitTerminalAsk: vi.fn(),
  submitTerminalBuyNow: vi.fn(),
}));

vi.mock("../tradingPetsApi", async (importOriginal) => {
  const actual = await importOriginal<typeof import("../tradingPetsApi")>();
  return {
    ...actual,
    submitTerminalBid: mocks.submitTerminalBid,
    submitTerminalAsk: mocks.submitTerminalAsk,
    submitTerminalBuyNow: mocks.submitTerminalBuyNow,
  };
});

const market: TerminalMarketRowDto = {
  marketEntryId: "m1",
  displayName: "Breed A",
  currentSupply: 2,
  latestTradePrice: 10,
  bestBidPrice: 9,
  bestAskPrice: 11,
  trendDirection: "Up",
  lastTradeAt: new Date().toISOString(),
};

const account: TerminalAccountSummaryDto = {
  displayName: "Demo",
  availableCash: 100,
  lockedCash: 0,
  portfolioTotal: 100,
  ownedQuantity: 1,
  eligibleAskQuantity: 1,
};

const okResult = {
  requestId: "r1",
  action: "PlaceBid",
  requestedQuantity: 1,
  filledQuantity: 0,
  pendingQuantity: 1,
  rejectedQuantity: 0,
  averageExecutedPrice: null as number | null,
  message: "Bid accepted.",
  affectedTradeIds: [] as string[],
};

describe("TerminalTradingPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.submitTerminalBid.mockResolvedValue(okResult);
    mocks.submitTerminalAsk.mockResolvedValue({
      ...okResult,
      action: "PlaceAsk",
      message: "Listed.",
    });
    mocks.submitTerminalBuyNow.mockResolvedValue({
      ...okResult,
      action: "BuyNow",
      message: "Filled.",
    });
  });

  it("shows validation error when quantity is invalid", async () => {
    const user = userEvent.setup();
    render(
      <TerminalTradingPanel
        marketEntry={market}
        accountSummary={account}
        loading={false}
        error={null}
        accessToken="tok"
      />,
    );

    await user.clear(screen.getByLabelText(/quantity/i));
    await user.type(screen.getByLabelText(/quantity/i), "0");
    await user.click(screen.getByRole("button", { name: /submit order/i }));

    expect(
      await screen.findByText(/whole quantity of at least 1/i),
    ).toBeInTheDocument();
    expect(mocks.submitTerminalBid).not.toHaveBeenCalled();
  });

  it("shows validation error when limit price missing for bid", async () => {
    const user = userEvent.setup();
    render(
      <TerminalTradingPanel
        marketEntry={market}
        accountSummary={account}
        loading={false}
        error={null}
        accessToken="tok"
      />,
    );

    await user.clear(screen.getByLabelText(/limit price/i));
    await user.click(screen.getByRole("button", { name: /submit order/i }));

    expect(
      await screen.findByText(/limit price greater than zero/i),
    ).toBeInTheDocument();
    expect(mocks.submitTerminalBid).not.toHaveBeenCalled();
  });

  it("does not require limit price for buy now", async () => {
    const user = userEvent.setup();
    render(
      <TerminalTradingPanel
        marketEntry={market}
        accountSummary={account}
        loading={false}
        error={null}
        accessToken="tok"
      />,
    );

    await user.click(screen.getByLabelText(/buy now/i));
    expect(screen.queryByLabelText(/limit price/i)).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /submit order/i }));

    await waitFor(() => {
      expect(mocks.submitTerminalBuyNow).toHaveBeenCalledWith(
        { marketEntryId: "m1", quantity: 1 },
        "tok",
      );
    });
  });

  it("shows success outcome and calls onOrderSettled", async () => {
    const user = userEvent.setup();
    const onOrderSettled = vi.fn().mockResolvedValue(undefined);
    render(
      <TerminalTradingPanel
        marketEntry={market}
        accountSummary={account}
        loading={false}
        error={null}
        accessToken="tok"
        onOrderSettled={onOrderSettled}
      />,
    );

    await user.type(screen.getByLabelText(/limit price/i), "12.5");
    await user.click(screen.getByRole("button", { name: /submit order/i }));

    expect(await screen.findByText("Bid accepted.")).toBeInTheDocument();
    await waitFor(() => {
      expect(onOrderSettled).toHaveBeenCalledTimes(1);
    });
    expect(mocks.submitTerminalBid).toHaveBeenCalledWith(
      { marketEntryId: "m1", quantity: 1, limitPrice: 12.5 },
      "tok",
    );
  });

  it("shows API error outcome and does not call onOrderSettled", async () => {
    const user = userEvent.setup();
    mocks.submitTerminalBid.mockRejectedValue(new Error("Insufficient funds"));
    const onOrderSettled = vi.fn();
    render(
      <TerminalTradingPanel
        marketEntry={market}
        accountSummary={account}
        loading={false}
        error={null}
        accessToken="tok"
        onOrderSettled={onOrderSettled}
      />,
    );

    await user.type(screen.getByLabelText(/limit price/i), "5");
    await user.click(screen.getByRole("button", { name: /submit order/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Insufficient funds",
    );
    expect(onOrderSettled).not.toHaveBeenCalled();
  });

  it("disables submit while submitting", async () => {
    const user = userEvent.setup();
    let resolveBid!: (v: typeof okResult) => void;
    const pending = new Promise<typeof okResult>((r) => {
      resolveBid = r;
    });
    mocks.submitTerminalBid.mockReturnValue(pending);

    render(
      <TerminalTradingPanel
        marketEntry={market}
        accountSummary={account}
        loading={false}
        error={null}
        accessToken="tok"
      />,
    );

    await user.type(screen.getByLabelText(/limit price/i), "9");
    const submitBtn = screen.getByRole("button", { name: /submit order/i });
    await user.click(submitBtn);

    expect(
      screen.getByRole("button", { name: /submitting/i }),
    ).toBeDisabled();

    resolveBid(okResult);
    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: /submit order/i }),
      ).not.toBeDisabled();
    });
  });
});
