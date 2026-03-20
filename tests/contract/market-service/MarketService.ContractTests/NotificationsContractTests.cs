using MarketService.Application.Pets;
using MarketService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class NotificationsContractTests
{
    private static readonly Guid PoodleBreedId = Guid.Parse("44444444-4444-4444-4444-000000000003");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");
    private static readonly Guid TraderTwo = Guid.Parse("33333333-3333-3333-3333-000000000002");

    [Fact]
    public async Task Cross_trade_creates_notifications_for_both_traders()
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
        await store.PlaceBidAsync(TraderTwo, listingId, 200m);

        var n1 = await store.GetNotificationsAsync(TraderOne, 50);
        var n2 = await store.GetNotificationsAsync(TraderTwo, 50);
        Assert.Contains(n1, n => n.Type.Contains("Trade", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(n2, n => n.Type.Contains("Trade", StringComparison.OrdinalIgnoreCase));
    }
}
