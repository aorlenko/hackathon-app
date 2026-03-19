namespace Trading.Contracts.Http;

public sealed record TradeDto(
    Guid TradeId,
    string Symbol,
    decimal Price,
    int Quantity,
    DateTimeOffset ExecutedAtUtc,
    string BuyerUserId,
    string SellerUserId);
