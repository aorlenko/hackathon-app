using MarketService.Application.Pets;
using MarketService.Application.Pets.Terminal;
using MarketService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class PetsTerminalMarketsContractTests
{
    private static readonly Guid LabradorBreedId = Guid.Parse("44444444-4444-4444-4444-000000000001");

    [Fact]
    public async Task Terminal_markets_align_with_breed_catalog_and_stable_name_order()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null);

        var breeds = await store.GetBreedsWithSupplyAsync();
        var markets = await store.GetTerminalMarketsAsync();

        Assert.Equal(breeds.Count, markets.Count);
        Assert.Equal(
            breeds.OrderBy(b => b.Name).Select(b => b.Id).ToList(),
            markets.Select(m => m.MarketEntryId).ToList());

        var labrador = markets.First(m => m.DisplayName == "Labrador");
        Assert.Equal(LabradorBreedId, labrador.MarketEntryId);
        Assert.True(labrador.CurrentSupply >= 0);
        Assert.Contains(labrador.TrendDirection, Enum.GetValues<TerminalTrendDirection>());
    }
}
