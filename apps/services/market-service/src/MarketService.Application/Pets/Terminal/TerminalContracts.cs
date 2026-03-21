namespace MarketService.Application.Pets.Terminal;

public enum TerminalTrendDirection
{
    Up = 0,
    Down = 1,
    Flat = 2,
    NoTradeData = 3
}

public enum TerminalOrderAction
{
    PlaceBid = 0,
    PlaceAsk = 1,
    BuyNow = 2
}

public sealed record TerminalMarketRow(
    Guid MarketEntryId,
    string DisplayName,
    int CurrentSupply,
    decimal? LatestTradePrice,
    decimal? BestBidPrice,
    decimal? BestAskPrice,
    TerminalTrendDirection TrendDirection,
    DateTimeOffset? LastTradeAt);

public sealed record TerminalOrderBookLevel(
    decimal Price,
    int Quantity,
    int OrderCount,
    DateTimeOffset OldestOrderAt);

public sealed record TerminalOrderBookSnapshot(
    Guid MarketEntryId,
    IReadOnlyList<TerminalOrderBookLevel> Bids,
    IReadOnlyList<TerminalOrderBookLevel> Asks,
    DateTimeOffset CapturedAt);

public sealed record TerminalAccountSummary(
    Guid TraderId,
    string DisplayName,
    decimal AvailableCash,
    decimal LockedCash,
    decimal PortfolioTotal,
    int OwnedQuantity,
    int EligibleAskQuantity);

public sealed record TerminalTradeRow(
    Guid TradeId,
    Guid MarketEntryId,
    decimal Price,
    int Quantity,
    DateTimeOffset ExecutedAt,
    string ExecutionType);

public sealed record TerminalWorkspaceSnapshot(
    TerminalMarketRow MarketEntry,
    TerminalOrderBookSnapshot OrderBook,
    TerminalAccountSummary AccountSummary,
    IReadOnlyList<TerminalTradeRow> RecentTrades,
    DateTimeOffset LastUpdatedAt);

public sealed record PlaceTerminalOrderRequest(
    Guid MarketEntryId,
    int Quantity,
    decimal? LimitPrice);

public sealed record TerminalOrderResult(
    Guid RequestId,
    TerminalOrderAction Action,
    int RequestedQuantity,
    int FilledQuantity,
    int PendingQuantity,
    int RejectedQuantity,
    decimal? AverageExecutedPrice,
    string Message,
    IReadOnlyList<Guid> AffectedTradeIds,
    IReadOnlyList<Guid> AffectedTraderIds);
