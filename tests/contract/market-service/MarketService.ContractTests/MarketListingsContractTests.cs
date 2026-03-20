using MarketService.Application.Pets;
using MarketService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class MarketListingsContractTests
{
    private static readonly Guid PoodleBreedId = Guid.Parse("44444444-4444-4444-4444-000000000003");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");
    private static readonly Guid TraderTwo = Guid.Parse("33333333-3333-3333-3333-000000000002");

    [Fact]
    public async Task Duplicate_active_listing_for_same_pet_is_rejected()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null);

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 1);
        var petId = (await store.GetTraderSnapshotAsync(TraderOne))!.Pets[0].Id;

        var first = await store.CreateListingAsync(TraderOne, petId, 120m);
        var second = await store.CreateListingAsync(TraderOne, petId, 130m);

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public async Task Withdraw_listing_releases_below_ask_bid()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null);

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 1);
        var petId = (await store.GetTraderSnapshotAsync(TraderOne))!.Pets[0].Id;
        var listingId = (await store.CreateListingAsync(TraderOne, petId, 200m))!.Value;

        await store.PlaceBidAsync(TraderTwo, listingId, 50m);
        var lockedBefore = (await store.GetTraderSnapshotAsync(TraderTwo))!.LockedCash;
        Assert.True(lockedBefore > 0);

        var ok = await store.WithdrawListingAsync(TraderOne, listingId);
        Assert.True(ok);

        var lockedAfter = (await store.GetTraderSnapshotAsync(TraderTwo))!.LockedCash;
        Assert.Equal(0m, lockedAfter);
    }
}
