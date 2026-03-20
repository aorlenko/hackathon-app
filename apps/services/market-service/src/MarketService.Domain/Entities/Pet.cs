namespace MarketService.Domain.Entities;

public sealed class Pet
{
    public Guid Id { get; set; }
    public Guid BreedId { get; set; }
    public Guid OwnerTraderId { get; set; }
    public decimal AgeYears { get; set; }
    public decimal Health { get; set; }
    public int CurrentDesirability { get; set; }
    public bool IsExpired { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Breed? Breed { get; set; }
    public Trader? Owner { get; set; }
}
