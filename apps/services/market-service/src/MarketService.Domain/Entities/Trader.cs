namespace MarketService.Domain.Entities;

public sealed class Trader
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ExternalUserId { get; set; }
    public decimal AvailableCash { get; set; }
    public decimal LockedCash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Pet> Pets { get; set; } = new List<Pet>();
    public ICollection<Listing> ListingsAsSeller { get; set; } = new List<Listing>();
    public ICollection<Bid> BidsAsBuyer { get; set; } = new List<Bid>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
