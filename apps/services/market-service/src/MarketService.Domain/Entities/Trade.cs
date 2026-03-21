namespace MarketService.Domain.Entities;

public sealed class Trade
{
    public Guid Id { get; set; }
    public Guid PetId { get; set; }
    public Guid ListingId { get; set; }
    public Guid BuyerTraderId { get; set; }
    public Guid SellerTraderId { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset ExecutedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Pet? Pet { get; set; }
    public Listing? Listing { get; set; }
    public Trader? Buyer { get; set; }
    public Trader? Seller { get; set; }
}
