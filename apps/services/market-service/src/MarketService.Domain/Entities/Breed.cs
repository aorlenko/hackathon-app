namespace MarketService.Domain.Entities;

public sealed class Breed
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PetBreedCategory Category { get; set; }
    public decimal LifespanYears { get; set; }
    public int BaselineDesirability { get; set; }
    public decimal MaintenanceCost { get; set; }
    public decimal RetailPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Supply? Supply { get; set; }
    public ICollection<Pet> Pets { get; set; } = new List<Pet>();
}
