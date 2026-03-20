using MarketService.Application.Pets.Terminal;
using Trading.TestSupport;

namespace MarketService.UnitTests;

public sealed class PetTradingTerminalProjectionTests
{
    private static readonly Guid LabradorBreedId = Guid.Parse("44444444-4444-4444-4444-000000000001");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");
    private static readonly Guid TraderTwo = Guid.Parse("33333333-3333-3333-3333-000000000002");

    [Fact]
    public async Task Workspace_order_book_aggregates_levels_by_price()
    {
        var harness = new TradingPlatformHarness();
        var store = harness.CreateMarketPetStore();

        await store.PurchasePetsAsync(TraderOne, LabradorBreedId, 2);
        var ownedPets = (await store.GetTraderSnapshotAsync(TraderOne))!.Pets
            .Where(p => p.BreedName == "Labrador")
            .Take(2)
            .ToList();

        var listingOne = await store.CreateListingAsync(TraderOne, ownedPets[0].Id, 80m);
        var listingTwo = await store.CreateListingAsync(TraderOne, ownedPets[1].Id, 80m);
        Assert.NotNull(listingOne);
        Assert.NotNull(listingTwo);

        var bidOne = await store.PlaceBidAsync(TraderTwo, listingOne!.Value, 60m);
        var bidTwo = await store.PlaceBidAsync(TraderTwo, listingTwo!.Value, 60m);
        Assert.NotNull(bidOne);
        Assert.NotNull(bidTwo);

        var workspace = await store.GetTerminalWorkspaceAsync(TraderOne, LabradorBreedId);

        Assert.NotNull(workspace);
        var askLevel = Assert.Single(workspace!.OrderBook.Asks);
        Assert.Equal(80m, askLevel.Price);
        Assert.Equal(2, askLevel.Quantity);
        Assert.Equal(2, askLevel.OrderCount);

        var bidLevel = Assert.Single(workspace.OrderBook.Bids);
        Assert.Equal(60m, bidLevel.Price);
        Assert.Equal(2, bidLevel.Quantity);
        Assert.Equal(2, bidLevel.OrderCount);
    }

    [Fact]
    public async Task Markets_derive_upward_trend_from_last_two_completed_trades()
    {
        var harness = new TradingPlatformHarness();
        var store = harness.CreateMarketPetStore();

        await store.PurchasePetsAsync(TraderOne, LabradorBreedId, 1);
        var firstPet = (await store.GetTraderSnapshotAsync(TraderOne))!.Pets
            .Where(p => p.BreedName == "Labrador")
            .OrderBy(p => p.Id)
            .First();
        var firstListing = await store.CreateListingAsync(TraderOne, firstPet.Id, 90m);
        Assert.NotNull(firstListing);
        var firstTrade = await store.PlaceBidAsync(TraderTwo, firstListing!.Value, 90m);
        Assert.NotNull(firstTrade);

        await store.PurchasePetsAsync(TraderTwo, LabradorBreedId, 1);
        var secondPet = (await store.GetTraderSnapshotAsync(TraderTwo))!.Pets
            .Where(p => p.BreedName == "Labrador")
            .OrderBy(p => p.Id)
            .First();
        var secondListing = await store.CreateListingAsync(TraderTwo, secondPet.Id, 120m);
        Assert.NotNull(secondListing);
        var secondTrade = await store.PlaceBidAsync(TraderOne, secondListing!.Value, 120m);
        Assert.NotNull(secondTrade);

        var markets = await store.GetTerminalMarketsAsync();
        var labrador = markets.Single(m => m.MarketEntryId == LabradorBreedId);

        Assert.Equal(120m, labrador.LatestTradePrice);
        Assert.Equal(TerminalTrendDirection.Up, labrador.TrendDirection);
        Assert.NotNull(labrador.LastTradeAt);
    }
}
