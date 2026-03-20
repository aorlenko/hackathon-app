namespace MarketService.Domain.Entities;

public enum BidStatus
{
    Active = 0,
    Withdrawn = 1,
    Rejected = 2,
    Superseded = 3,
    Accepted = 4
}

public sealed class Bid
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid BuyerTraderId { get; set; }
    public decimal Amount { get; set; }
    public BidStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Listing? Listing { get; set; }
    public Trader? Buyer { get; set; }
}
