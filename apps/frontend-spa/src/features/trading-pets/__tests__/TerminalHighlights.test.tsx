import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { TerminalMarketList } from "../terminal/TerminalMarketList";
import { TerminalOrderBook } from "../terminal/TerminalOrderBook";
import { TerminalTradeFeed } from "../terminal/TerminalTradeFeed";
import {
  buildTerminalMarketHighlights,
  buildTerminalWorkspaceHighlights,
} from "../terminal/terminalHighlights";
import type { TerminalMarketRowDto, TerminalWorkspaceDto } from "../tradingPetsApi";

const baseMarket: TerminalMarketRowDto = {
  marketEntryId: "m1",
  displayName: "Breed A",
  currentSupply: 3,
  latestTradePrice: 10,
  bestBidPrice: 9,
  bestAskPrice: 11,
  trendDirection: "Flat",
  lastTradeAt: "2026-03-21T12:00:00.000Z",
};

const makeWorkspace = (
  overrides?: Partial<TerminalWorkspaceDto>,
): TerminalWorkspaceDto => ({
  marketEntry: baseMarket,
  orderBook: {
    capturedAt: "2026-03-21T12:00:00.000Z",
    bids: [{ price: 9, quantity: 1, orderCount: 1 }],
    asks: [{ price: 11, quantity: 1, orderCount: 1 }],
  },
  accountSummary: {
    displayName: "Demo",
    availableCash: 100,
    lockedCash: 0,
    portfolioTotal: 100,
    ownedQuantity: 1,
    eligibleAskQuantity: 1,
  },
  recentTrades: [
    {
      tradeId: "t1",
      price: 10,
      quantity: 1,
      executedAt: "2026-03-21T12:00:00.000Z",
      executionType: "BuyNow",
    },
  ],
  lastUpdatedAt: "2026-03-21T12:00:00.000Z",
  ...overrides,
});

describe("terminal highlights", () => {
  it("detects changed market prices and trend direction", () => {
    const nextMarket: TerminalMarketRowDto = {
      ...baseMarket,
      latestTradePrice: 12,
      bestBidPrice: 10,
      bestAskPrice: 10,
      trendDirection: "Up",
    };

    expect(buildTerminalMarketHighlights([baseMarket], [nextMarket])).toEqual({
      m1: {
        latestTradePrice: "up",
        bestBidPrice: "up",
        bestAskPrice: "down",
        trendChanged: true,
      },
    });
  });

  it("detects new trades and order-book level changes", () => {
    const previousWorkspace = makeWorkspace();
    const nextWorkspace = makeWorkspace({
      orderBook: {
        capturedAt: "2026-03-21T12:00:05.000Z",
        bids: [
          { price: 9, quantity: 2, orderCount: 1 },
          { price: 8, quantity: 1, orderCount: 1 },
        ],
        asks: [{ price: 11, quantity: 1, orderCount: 2 }],
      },
      recentTrades: [
        {
          tradeId: "t2",
          price: 12,
          quantity: 1,
          executedAt: "2026-03-21T12:00:05.000Z",
          executionType: "CrossingBid",
        },
        ...previousWorkspace.recentTrades,
      ],
    });

    expect(
      buildTerminalWorkspaceHighlights(previousWorkspace, nextWorkspace),
    ).toEqual({
      newTradeIds: ["t2"],
      bidLevels: {
        "9.00": { change: "up" },
        "8.00": { change: "new" },
      },
      askLevels: {
        "11.00": { change: "up" },
      },
    });
  });

  it("renders visual highlight classes for markets, depth, and new trades", () => {
    render(
      <>
        <TerminalMarketList
          markets={[baseMarket]}
          highlights={{
            m1: {
              latestTradePrice: "up",
              bestBidPrice: "up",
              bestAskPrice: "down",
              trendChanged: true,
            },
          }}
          selectedMarketEntryId="m1"
          onSelect={() => {}}
          loading={false}
          error={null}
        />
        <TerminalOrderBook
          marketEntry={baseMarket}
          orderBook={makeWorkspace().orderBook}
          bidHighlights={{ "9.00": { change: "up" } }}
          askHighlights={{ "11.00": { change: "new" } }}
          loading={false}
          error={null}
        />
        <TerminalTradeFeed
          marketLabel="Breed A"
          trades={makeWorkspace().recentTrades}
          newTradeIds={["t1"]}
          loading={false}
          error={null}
        />
      </>,
    );

    expect(
      screen.getByRole("button", { name: /Breed A/i }),
    ).toHaveClass("trading-pets-terminal__market-row--active");
    expect(screen.getByText("Last $10.00")).toHaveClass(
      "trading-pets-terminal__flash--up",
    );
    expect(screen.getByText("Ask $11.00")).toHaveClass(
      "trading-pets-terminal__flash--down",
    );
    expect(screen.getByTitle("Trend")).toHaveClass(
      "trading-pets-terminal__trend--active",
    );
    expect(screen.getByText("$9.00").closest("tr")).toHaveClass(
      "trading-pets-terminal__book-row--up",
    );
    expect(screen.getByText("$11.00").closest("tr")).toHaveClass(
      "trading-pets-terminal__book-row--new",
    );
    expect(screen.getByText(/\u00d7 1 · BuyNow/i).closest("li")).toHaveClass(
      "trading-pets-terminal__trade-row--new",
    );
  });
});
