using MarketService.Application.Pets.Terminal;
using Trading.TestSupport;
namespace MarketService.ContractTests;

public sealed class PetsTerminalOrdersContractTests
{
    private static readonly Guid PoodleBreedId = Guid.Parse("44444444-4444-4444-4444-000000000003");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");
    private static readonly Guid TraderTwo = Guid.Parse("33333333-3333-3333-3333-000000000002");

    [Fact]
    public async Task Place_terminal_ask_returns_pending_listing_result_shape()
    {
        var harness = new TradingPlatformHarness();
        var store = harness.CreateMarketPetStore();

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 2);

        var result = await store.PlaceTerminalAskAsync(TraderOne, PoodleBreedId, 2, 130m);

        Assert.NotNull(result);
        Assert.Equal(TerminalOrderAction.PlaceAsk, result!.Action);
        Assert.Equal(2, result.RequestedQuantity);
        Assert.Equal(2, result.FilledQuantity);
        Assert.Equal(0, result.PendingQuantity);
        Assert.Equal(0, result.RejectedQuantity);
        Assert.Null(result.AverageExecutedPrice);
        Assert.Empty(result.AffectedTradeIds);
    }

    [Fact]
    public async Task Place_terminal_bid_can_mix_immediate_fill_and_pending_bid()
    {
        var harness = new TradingPlatformHarness();
        var store = harness.CreateMarketPetStore();

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 2);
        var ownedPets = (await store.GetTraderSnapshotAsync(TraderOne))!.Pets
            .Where(p => p.BreedName == "Poodle")
            .OrderBy(p => p.Id)
            .Take(2)
            .ToList();

        await store.CreateListingAsync(TraderOne, ownedPets[0].Id, 80m);
        await store.CreateListingAsync(TraderOne, ownedPets[1].Id, 120m);

        var result = await store.PlaceTerminalBidAsync(TraderTwo, PoodleBreedId, 2, 100m);

        Assert.NotNull(result);
        Assert.Equal(TerminalOrderAction.PlaceBid, result!.Action);
        Assert.Equal(2, result.RequestedQuantity);
        Assert.Equal(1, result.FilledQuantity);
        Assert.Equal(1, result.PendingQuantity);
        Assert.Equal(0, result.RejectedQuantity);
        Assert.Equal(80m, result.AverageExecutedPrice);
        Assert.Single(result.AffectedTradeIds);
    }

    [Fact]
    public async Task Buy_now_returns_filled_result_for_best_available_asks()
    {
        var harness = new TradingPlatformHarness();
        var store = harness.CreateMarketPetStore();

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 2);
        var ownedPets = (await store.GetTraderSnapshotAsync(TraderOne))!.Pets
            .Where(p => p.BreedName == "Poodle")
            .OrderBy(p => p.Id)
            .Take(2)
            .ToList();

        await store.CreateListingAsync(TraderOne, ownedPets[0].Id, 70m);
        await store.CreateListingAsync(TraderOne, ownedPets[1].Id, 90m);

        var result = await store.BuyNowAsync(TraderTwo, PoodleBreedId, 2);

        Assert.NotNull(result);
        Assert.Equal(TerminalOrderAction.BuyNow, result!.Action);
        Assert.Equal(2, result.RequestedQuantity);
        Assert.Equal(2, result.FilledQuantity);
        Assert.Equal(0, result.PendingQuantity);
        Assert.Equal(0, result.RejectedQuantity);
        Assert.Equal(80m, result.AverageExecutedPrice);
        Assert.Equal(2, result.AffectedTradeIds.Count);
    }

    [Fact]
    public async Task PlaceTerminalOrderHandler_rejects_bid_and_ask_without_valid_limit()
    {
        var harness = new TradingPlatformHarness();
        var store = harness.CreateMarketPetStore();
        var handler = new PlaceTerminalOrderHandler(store);

        Assert.Null(await handler.PlaceBidAsync(TraderOne, new PlaceTerminalOrderRequest(PoodleBreedId, 1, null)));
        Assert.Null(await handler.PlaceAskAsync(TraderOne, new PlaceTerminalOrderRequest(PoodleBreedId, 1, 0)));
    }
}
