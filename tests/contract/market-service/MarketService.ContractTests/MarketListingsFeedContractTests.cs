using MarketService.Application.Pets;
using MarketService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class MarketListingsFeedContractTests
{
    private static readonly Guid PoodleBreedId = Guid.Parse("44444444-4444-4444-4444-000000000003");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");

    [Fact]
    public async Task Market_listings_include_supply_and_recent_trade_fields()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null);

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 1);
        var snapshot = await store.GetTraderSnapshotAsync(TraderOne);
        var petId = snapshot!.Pets[0].Id;
        await store.CreateListingAsync(TraderOne, petId, 150m);

        var rows = await store.GetMarketListingsAsync();
        Assert.NotEmpty(rows);
        var row = rows.First(r => r.PetId == petId);
        Assert.True(row.AskingPrice > 0);
        Assert.False(string.IsNullOrWhiteSpace(row.BreedName));
        Assert.True(row.RemainingNewSupplyForBreed >= 0);
    }
}
