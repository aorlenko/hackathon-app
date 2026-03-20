using MarketService.Domain.Entities;

namespace MarketService.Infrastructure.Persistence;

internal static class MarketPetSeedData
{
    public static Guid BreedGuid(int index) => Guid.Parse($"44444444-4444-4444-4444-{index:D12}");

    public static Guid TraderGuid(int index) => Guid.Parse($"33333333-3333-3333-3333-{index:D12}");

    public static IReadOnlyList<Breed> CreateBreeds(int defaultSupplyPerBreed)
    {
        var rows = new List<(int Id, string Name, PetBreedCategory Cat, decimal Life, int Des, decimal Maint, decimal Price)>
        {
            (1, "Labrador", PetBreedCategory.Dog, 12, 8, 5, 100),
            (2, "Beagle", PetBreedCategory.Dog, 13, 7, 4, 90),
            (3, "Poodle", PetBreedCategory.Dog, 14, 9, 6, 110),
            (4, "Bulldog", PetBreedCategory.Dog, 10, 6, 7, 80),
            (5, "Pit Bull", PetBreedCategory.Dog, 11, 5, 5, 70),
            (6, "Siamese", PetBreedCategory.Cat, 15, 9, 4, 90),
            (7, "Persian", PetBreedCategory.Cat, 14, 8, 6, 85),
            (8, "Maine Coon", PetBreedCategory.Cat, 16, 7, 5, 80),
            (9, "Bengal", PetBreedCategory.Cat, 12, 6, 5, 75),
            (10, "Sphynx", PetBreedCategory.Cat, 13, 5, 7, 70),
            (11, "Parakeet", PetBreedCategory.Bird, 8, 7, 3, 25),
            (12, "Canary", PetBreedCategory.Bird, 10, 6, 2, 20),
            (13, "Cockatiel", PetBreedCategory.Bird, 12, 8, 3, 30),
            (14, "Macaw", PetBreedCategory.Bird, 50, 9, 8, 120),
            (15, "Lovebird", PetBreedCategory.Bird, 15, 5, 3, 15),
            (16, "Goldfish", PetBreedCategory.Fish, 10, 5, 2, 5),
            (17, "Betta", PetBreedCategory.Fish, 5, 6, 1, 6),
            (18, "Guppy", PetBreedCategory.Fish, 3, 4, 1, 4),
            (19, "Angelfish", PetBreedCategory.Fish, 8, 7, 2, 8),
            (20, "Clownfish", PetBreedCategory.Fish, 6, 8, 3, 10)
        };

        return rows.Select(r => new Breed
        {
            Id = BreedGuid(r.Id),
            Name = r.Name,
            Category = r.Cat,
            LifespanYears = r.Life,
            BaselineDesirability = r.Des,
            MaintenanceCost = r.Maint,
            RetailPrice = r.Price,
            Supply = new Supply
            {
                BreedId = BreedGuid(r.Id),
                RemainingCount = defaultSupplyPerBreed
            }
        }).ToList();
    }
}
