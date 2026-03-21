using MarketService.Application.Pets.Terminal;
using Trading.TestSupport;

namespace MarketTrade.IntegrationTests;

/// <summary>
/// Pet terminal order flows against <see cref="MarketService.Infrastructure.Persistence.MarketPetDataStore"/>
/// (same pattern as market contract tests; no HTTP host).
/// </summary>
public sealed class PetTradingTerminalOrderFlowTests
{
    private static readonly Guid BeagleBreedId = Guid.Parse("44444444-4444-4444-4444-000000000002");
    private static readonly Guid PoodleBreedId = Guid.Parse("44444444-4444-4444-4444-000000000003");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");
    private static readonly Guid TraderTwo = Guid.Parse("33333333-3333-3333-3333-000000000002");

    [Fact]
    public async Task Terminal_orders_reconcile_workspace_and_snapshot_state()
    {
        var harness = new TradingPlatformHarness();
        var store = harness.CreateMarketPetStore();

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 2);

        var ask = await store.PlaceTerminalAskAsync(TraderOne, PoodleBreedId, 2, 100m);
        Assert.NotNull(ask);
        Assert.Equal(2, ask!.FilledQuantity);
        Assert.Equal(0, ask.PendingQuantity);

        var sellerWorkspaceAfterAsk = await store.GetTerminalWorkspaceAsync(TraderOne, PoodleBreedId);
        Assert.NotNull(sellerWorkspaceAfterAsk);
        Assert.Single(sellerWorkspaceAfterAsk!.OrderBook.Asks);
        Assert.Equal(2, sellerWorkspaceAfterAsk.OrderBook.Asks[0].Quantity);
        Assert.Empty(sellerWorkspaceAfterAsk.OrderBook.Bids);

        var bid = await store.PlaceTerminalBidAsync(TraderTwo, PoodleBreedId, 1, 90m);
        Assert.NotNull(bid);
        Assert.Equal(0, bid!.FilledQuantity);
        Assert.Equal(1, bid.PendingQuantity);

        var buyerWorkspaceAfterBid = await store.GetTerminalWorkspaceAsync(TraderTwo, PoodleBreedId);
        Assert.NotNull(buyerWorkspaceAfterBid);
        Assert.Single(buyerWorkspaceAfterBid!.OrderBook.Bids);
        Assert.Equal(1, buyerWorkspaceAfterBid.OrderBook.Bids[0].Quantity);
        Assert.Equal(90m, buyerWorkspaceAfterBid.OrderBook.Bids[0].Price);
        Assert.Equal(90m, buyerWorkspaceAfterBid.AccountSummary.LockedCash);

        var buyNow = await store.BuyNowAsync(TraderTwo, PoodleBreedId, 1);
        Assert.NotNull(buyNow);
        Assert.Equal(1, buyNow!.FilledQuantity);
        Assert.Single(buyNow.AffectedTradeIds);

        var buyerWorkspaceAfterBuyNow = await store.GetTerminalWorkspaceAsync(TraderTwo, PoodleBreedId);
        Assert.NotNull(buyerWorkspaceAfterBuyNow);
        Assert.Single(buyerWorkspaceAfterBuyNow!.OrderBook.Asks);
        Assert.Empty(buyerWorkspaceAfterBuyNow.OrderBook.Bids);
        Assert.Single(buyerWorkspaceAfterBuyNow.RecentTrades);
        Assert.Equal(1, buyerWorkspaceAfterBuyNow.AccountSummary.OwnedQuantity);
        Assert.Equal(0m, buyerWorkspaceAfterBuyNow.AccountSummary.LockedCash);

        var buyerSnapshot = await store.GetTraderSnapshotAsync(TraderTwo);
        Assert.NotNull(buyerSnapshot);
        Assert.Single(buyerSnapshot!.Pets, p => p.BreedName == "Poodle");
    }

    [Fact]
    public async Task Terminal_ask_cross_bid_and_buy_now_reconcile_trader_snapshots()
    {
        var harness = new TradingPlatformHarness();
        var store = harness.CreateMarketPetStore();

        await store.PurchasePetsAsync(TraderOne, BeagleBreedId, 1);
        await store.PurchasePetsAsync(TraderTwo, BeagleBreedId, 1);

        var seller = await store.GetTraderSnapshotAsync(TraderOne);
        Assert.NotNull(seller);
        var listedPet = seller!.Pets[0].Id;

        var ask = await store.PlaceTerminalAskAsync(TraderOne, BeagleBreedId, 1, 95m);
        Assert.NotNull(ask);
        Assert.Equal(1, ask!.FilledQuantity);

        var bid = await store.PlaceTerminalBidAsync(TraderTwo, BeagleBreedId, 1, 95m);
        Assert.NotNull(bid);
        Assert.Equal(1, bid!.FilledQuantity);
        Assert.Equal(0, bid.PendingQuantity);

        var sellerAfter = await store.GetTraderSnapshotAsync(TraderOne);
        var buyerAfter = await store.GetTraderSnapshotAsync(TraderTwo);
        Assert.NotNull(sellerAfter);
        Assert.NotNull(buyerAfter);
        Assert.DoesNotContain(sellerAfter!.Pets, p => p.Id == listedPet);
        Assert.Contains(buyerAfter!.Pets, p => p.Id == listedPet);

        await store.PurchasePetsAsync(TraderOne, BeagleBreedId, 1);
        var seller2 = await store.GetTraderSnapshotAsync(TraderOne);
        Assert.NotNull(seller2);
        var newPet = Assert.Single(seller2!.Pets, p => p.Id != listedPet);
        await store.CreateListingAsync(TraderOne, newPet.Id, 88m);

        var buyNow = await store.BuyNowAsync(TraderTwo, BeagleBreedId, 1);
        Assert.NotNull(buyNow);
        Assert.Equal(1, buyNow!.FilledQuantity);
        Assert.Equal(88m, buyNow.AverageExecutedPrice);
    }
}
