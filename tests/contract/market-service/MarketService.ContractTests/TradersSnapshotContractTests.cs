using MarketService.Application.Pets;
using MarketService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class TradersSnapshotContractTests
{
    private static readonly Guid LabradorBreedId = Guid.Parse("44444444-4444-4444-4444-000000000001");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");
    private static readonly Guid TraderTwo = Guid.Parse("33333333-3333-3333-3333-000000000002");

    [Fact]
    public async Task Trader_A_purchase_does_not_change_trader_B_snapshot()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null,
            null);

        var beforeB = await store.GetTraderSnapshotAsync(TraderTwo);
        Assert.NotNull(beforeB);

        await store.PurchasePetsAsync(TraderOne, LabradorBreedId, 2);

        var afterA = await store.GetTraderSnapshotAsync(TraderOne);
        var afterB = await store.GetTraderSnapshotAsync(TraderTwo);

        Assert.NotNull(afterA);
        Assert.NotNull(afterB);
        Assert.Equal(2, afterA!.Pets.Count);
        Assert.Empty(afterB!.Pets);
        Assert.Equal(beforeB!.AvailableCash, afterB.AvailableCash);
        Assert.Equal(beforeB.LockedCash, afterB.LockedCash);
    }

    [Fact]
    public async Task EnsureLinkedTraderForUserAsync_is_idempotent()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(new TradingPetsOptions()),
            null,
            null);

        var id1 = await store.EnsureLinkedTraderForUserAsync("auth0|linked-user", "Linked User");
        var id2 = await store.EnsureLinkedTraderForUserAsync("auth0|linked-user", "Linked User");

        Assert.Equal(id1, id2);
        var snap = await store.GetTraderSnapshotAsync(id1);
        Assert.NotNull(snap);
        Assert.Equal("Linked User", snap!.DisplayName);
        Assert.True(snap.AvailableCash > 0);
    }
}
