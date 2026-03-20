using MarketService.Application.Pets;
using MarketService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class PetsTerminalWorkspaceContractTests
{
    private static readonly Guid LabradorBreedId = Guid.Parse("44444444-4444-4444-4444-000000000001");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");

    [Fact]
    public async Task Terminal_workspace_returns_snapshot_for_known_market_and_null_for_unknown()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null);

        var workspace = await store.GetTerminalWorkspaceAsync(TraderOne, LabradorBreedId);
        Assert.NotNull(workspace);
        Assert.Equal(LabradorBreedId, workspace!.MarketEntry.MarketEntryId);
        Assert.Equal(workspace.MarketEntry.MarketEntryId, workspace.OrderBook.MarketEntryId);
        Assert.Equal(TraderOne, workspace.AccountSummary.TraderId);
        Assert.True(workspace.AccountSummary.PortfolioTotal >= 0);
        Assert.True(workspace.AccountSummary.OwnedQuantity >= 0);
        Assert.True(workspace.AccountSummary.EligibleAskQuantity >= 0);
        Assert.True(workspace.AccountSummary.EligibleAskQuantity <= workspace.AccountSummary.OwnedQuantity);
        Assert.Equal(workspace.OrderBook.CapturedAt, workspace.LastUpdatedAt);

        var missing = await store.GetTerminalWorkspaceAsync(
            TraderOne,
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        Assert.Null(missing);
    }
}
