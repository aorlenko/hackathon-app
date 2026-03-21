using MarketService.Application.Abstractions;
using MarketService.Application.Pets;
using MarketService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class MarketBidsContractTests
{
    private static readonly Guid PoodleBreedId = Guid.Parse("44444444-4444-4444-4444-000000000003");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");
    private static readonly Guid TraderTwo = Guid.Parse("33333333-3333-3333-3333-000000000002");

    [Fact]
    public async Task Crossing_bid_executes_immediate_trade()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null,
            null);

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 1);
        var snapshot = await store.GetTraderSnapshotAsync(TraderOne);
        var petId = snapshot!.Pets[0].Id;

        var listingId = await store.CreateListingAsync(TraderOne, petId, 200m);
        Assert.NotNull(listingId);

        var outcome = await store.PlaceBidAsync(TraderTwo, listingId!.Value, 200m);
        var cross = Assert.IsType<CrossTradePlaceResult>(outcome);
        Assert.Equal(petId, cross.Trade.PetId);
        Assert.Equal(TraderTwo, cross.Trade.BuyerTraderId);
        Assert.Equal(TraderOne, cross.Trade.SellerTraderId);
    }

    [Fact]
    public async Task Higher_below_ask_bid_releases_prior_bidder_lock()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null,
            null);

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 1);
        var petId = (await store.GetTraderSnapshotAsync(TraderOne))!.Pets[0].Id;
        var listingId = (await store.CreateListingAsync(TraderOne, petId, 200m))!.Value;

        await store.PlaceBidAsync(TraderTwo, listingId, 40m);
        var traderThree = Guid.Parse("33333333-3333-3333-3333-000000000003");
        await store.PlaceBidAsync(traderThree, listingId, 60m);

        var t2 = await store.GetTraderSnapshotAsync(TraderTwo);
        var t3 = await store.GetTraderSnapshotAsync(traderThree);
        Assert.NotNull(t2);
        Assert.NotNull(t3);
        Assert.Equal(0m, t2!.LockedCash);
        Assert.Equal(60m, t3!.LockedCash);
    }
}
