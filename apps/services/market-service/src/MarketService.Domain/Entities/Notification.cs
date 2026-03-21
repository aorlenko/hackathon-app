namespace MarketService.Domain.Entities;

public enum NotificationType
{
    BidReceived = 0,
    BidAccepted = 1,
    BidRejected = 2,
    BidWithdrawn = 3,
    Outbid = 4,
    ListingRemoved = 5,
    TradeCompleted = 6,
    /// <summary>Seller rejected a below-ask bid; recipient is the listing owner.</summary>
    SellerRejectedBid = 7
}

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid TraderId { get; set; }
    public NotificationType Type { get; set; }
    public Guid PetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public Guid CounterpartyTraderId { get; set; }
    public string CounterpartyDisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? Correlation { get; set; }

    public Trader? Trader { get; set; }
}
