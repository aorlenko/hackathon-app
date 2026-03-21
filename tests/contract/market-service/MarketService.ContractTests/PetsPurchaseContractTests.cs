using MarketService.Application.Pets;
using MarketService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class PetsPurchaseContractTests
{
    private static readonly Guid LabradorBreedId = Guid.Parse("44444444-4444-4444-4444-000000000001");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");

    [Fact]
    public async Task Purchase_succeeds_when_cash_and_supply_allow()
    {
        var harness = new TradingPlatformHarness();
        var store = CreateStore(harness);

        var result = await store.PurchasePetsAsync(TraderOne, LabradorBreedId, 1);

        Assert.NotNull(result);
        Assert.Single(result!.Pets);
        Assert.True(result.AvailableCash < 1000m);
        var breeds = await store.GetBreedsWithSupplyAsync();
        var labrador = breeds.First(b => b.Id == LabradorBreedId);
        Assert.Equal(2, labrador.RemainingSupply);
    }

    [Fact]
    public async Task Purchase_fails_when_supply_exhausted()
    {
        var harness = new TradingPlatformHarness();
        var store = CreateStore(harness);

        var first = await store.PurchasePetsAsync(TraderOne, LabradorBreedId, 3);
        Assert.NotNull(first);

        var second = await store.PurchasePetsAsync(TraderOne, LabradorBreedId, 1);
        Assert.Null(second);
    }

    [Fact]
    public async Task Purchase_fails_when_cash_insufficient()
    {
        var harness = new TradingPlatformHarness();
        var store = CreateStore(harness);

        var traderRow = await harness.MarketStore.Traders.FirstAsync(t => t.Id == TraderOne);
        traderRow.AvailableCash = 1m;
        await harness.MarketStore.SaveChangesAsync();

        var broke = await store.PurchasePetsAsync(TraderOne, LabradorBreedId, 1);
        Assert.Null(broke);
    }

    private static MarketPetDataStore CreateStore(TradingPlatformHarness harness) =>
        new(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null,
            null);
}
