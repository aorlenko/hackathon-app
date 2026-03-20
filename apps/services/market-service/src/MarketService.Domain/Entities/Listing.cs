namespace MarketService.Domain.Entities;

public sealed class Listing
{
    public Guid Id { get; set; }
    public Guid PetId { get; set; }
    public Guid SellerTraderId { get; set; }
    public decimal AskingPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? WithdrawnAt { get; set; }

    public Pet? Pet { get; set; }
    public Trader? Seller { get; set; }
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<Trade> Trades { get; set; } = new List<Trade>();
}
