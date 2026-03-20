using MarketService.Application.Pets;
using MarketService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class PetsBreedsContractTests
{
    [Fact]
    public async Task Breeds_feed_has_twenty_rows_with_remaining_supply_and_pricing_fields()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null);

        var rows = await store.GetBreedsWithSupplyAsync();

        Assert.Equal(20, rows.Count);
        Assert.All(
            rows,
            b =>
            {
                Assert.NotEqual(Guid.Empty, b.Id);
                Assert.False(string.IsNullOrWhiteSpace(b.Name));
                Assert.Contains(b.Category, new[] { "Dog", "Cat", "Bird", "Fish" });
                Assert.True(b.LifespanYears > 0);
                Assert.InRange(b.BaselineDesirability, 1, 10);
                Assert.True(b.RetailPrice > 0);
                Assert.True(b.RemainingSupply >= 0);
            });
        Assert.Contains(rows, b => b.Name == "Labrador" && b.RemainingSupply == 3);
    }
}
