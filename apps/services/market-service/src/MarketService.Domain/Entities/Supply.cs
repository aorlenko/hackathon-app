namespace MarketService.Domain.Entities;

public sealed class Supply
{
    public Guid BreedId { get; set; }
    public int RemainingCount { get; set; }

    public Breed? Breed { get; set; }
}
