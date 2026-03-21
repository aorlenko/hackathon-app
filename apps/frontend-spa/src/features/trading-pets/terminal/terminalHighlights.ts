import type {
  TerminalMarketRowDto,
  TerminalOrderBookDto,
  TerminalOrderBookLevelDto,
  TerminalWorkspaceDto,
} from "../tradingPetsApi";

export type PriceFlashDirection = "up" | "down";

export type TerminalMarketHighlight = {
  latestTradePrice?: PriceFlashDirection;
  bestBidPrice?: PriceFlashDirection;
  bestAskPrice?: PriceFlashDirection;
  trendChanged?: boolean;
};

export type TerminalOrderBookLevelHighlight = {
  change: "new" | PriceFlashDirection;
};

export type TerminalWorkspaceHighlights = {
  newTradeIds: string[];
  bidLevels: Record<string, TerminalOrderBookLevelHighlight>;
  askLevels: Record<string, TerminalOrderBookLevelHighlight>;
};

const getPriceFlashDirection = (
  previous: number | null | undefined,
  next: number | null | undefined,
): PriceFlashDirection | undefined => {
  if (previous == null || next == null || previous === next) {
    return undefined;
  }

  return next > previous ? "up" : "down";
};

export const getOrderBookLevelKey = (level: TerminalOrderBookLevelDto) =>
  level.price.toFixed(2);

export const buildTerminalMarketHighlights = (
  previousMarkets: TerminalMarketRowDto[],
  nextMarkets: TerminalMarketRowDto[],
): Record<string, TerminalMarketHighlight> => {
  const previousById = new Map(
    previousMarkets.map((market) => [market.marketEntryId, market]),
  );
  const highlights: Record<string, TerminalMarketHighlight> = {};

  for (const market of nextMarkets) {
    const previous = previousById.get(market.marketEntryId);
    if (!previous) {
      continue;
    }

    const highlight: TerminalMarketHighlight = {
      latestTradePrice: getPriceFlashDirection(
        previous.latestTradePrice,
        market.latestTradePrice,
      ),
      bestBidPrice: getPriceFlashDirection(
        previous.bestBidPrice,
        market.bestBidPrice,
      ),
      bestAskPrice: getPriceFlashDirection(
        previous.bestAskPrice,
        market.bestAskPrice,
      ),
      trendChanged: previous.trendDirection !== market.trendDirection,
    };

    if (
      highlight.latestTradePrice ||
      highlight.bestBidPrice ||
      highlight.bestAskPrice ||
      highlight.trendChanged
    ) {
      highlights[market.marketEntryId] = highlight;
    }
  }

  return highlights;
};

const buildLevelHighlights = (
  previousLevels: TerminalOrderBookLevelDto[],
  nextLevels: TerminalOrderBookLevelDto[],
): Record<string, TerminalOrderBookLevelHighlight> => {
  const previousByPrice = new Map(
    previousLevels.map((level) => [getOrderBookLevelKey(level), level]),
  );
  const highlights: Record<string, TerminalOrderBookLevelHighlight> = {};

  for (const level of nextLevels) {
    const key = getOrderBookLevelKey(level);
    const previous = previousByPrice.get(key);

    if (!previous) {
      highlights[key] = { change: "new" };
      continue;
    }

    if (previous.quantity !== level.quantity) {
      highlights[key] = {
        change: level.quantity > previous.quantity ? "up" : "down",
      };
      continue;
    }

    if (previous.orderCount !== level.orderCount) {
      highlights[key] = {
        change: level.orderCount > previous.orderCount ? "up" : "down",
      };
    }
  }

  return highlights;
};

export const buildTerminalWorkspaceHighlights = (
  previousWorkspace: TerminalWorkspaceDto | null,
  nextWorkspace: TerminalWorkspaceDto,
): TerminalWorkspaceHighlights => {
  if (
    !previousWorkspace ||
    previousWorkspace.marketEntry.marketEntryId !==
      nextWorkspace.marketEntry.marketEntryId
  ) {
    return {
      newTradeIds: [],
      bidLevels: {},
      askLevels: {},
    };
  }

  const previousTradeIds = new Set(
    previousWorkspace.recentTrades.map((trade) => trade.tradeId),
  );

  return {
    newTradeIds: nextWorkspace.recentTrades
      .filter((trade) => !previousTradeIds.has(trade.tradeId))
      .map((trade) => trade.tradeId),
    bidLevels: buildLevelHighlights(
      previousWorkspace.orderBook.bids,
      nextWorkspace.orderBook.bids,
    ),
    askLevels: buildLevelHighlights(
      previousWorkspace.orderBook.asks,
      nextWorkspace.orderBook.asks,
    ),
  };
};
