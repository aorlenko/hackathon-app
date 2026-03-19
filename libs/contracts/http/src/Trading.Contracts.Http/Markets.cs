namespace Trading.Contracts.Http;

public sealed record MarketSummaryDto(
    string Symbol,
    string Name,
    string Category,
    decimal ReferencePrice,
    bool IsTradable);

public sealed record OrderBookLevelDto(decimal Price, int Quantity, int OrderCount);

public sealed record OrderBookDto(
    string Symbol,
    IReadOnlyList<OrderBookLevelDto> Bids,
    IReadOnlyList<OrderBookLevelDto> Asks,
    DateTimeOffset LastUpdatedAtUtc);
