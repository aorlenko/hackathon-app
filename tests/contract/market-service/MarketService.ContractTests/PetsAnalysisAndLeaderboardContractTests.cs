using MarketService.Application.Pets;
using MarketService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class PetsAnalysisAndLeaderboardContractTests
{
    private static readonly Guid PoodleBreedId = Guid.Parse("44444444-4444-4444-4444-000000000003");
    private static readonly Guid TraderOne = Guid.Parse("33333333-3333-3333-3333-000000000001");

    [Fact]
    public async Task Analysis_visible_for_listed_pet_without_owner_context()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null);

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 1);
        var petId = (await store.GetTraderSnapshotAsync(TraderOne))!.Pets[0].Id;
        await store.CreateListingAsync(TraderOne, petId, 120m);

        var analysis = await store.GetPetAnalysisAsync(petId, viewerTraderId: null);
        Assert.NotNull(analysis);
        Assert.Equal(petId, analysis!.PetId);
        Assert.False(string.IsNullOrWhiteSpace(analysis.BreedName));
    }

    [Fact]
    public async Task Leaderboard_sorted_by_portfolio_descending()
    {
        var harness = new TradingPlatformHarness();
        var store = new MarketPetDataStore(
            harness.MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()),
            null);

        await store.PurchasePetsAsync(TraderOne, PoodleBreedId, 1);

        var board = await store.GetLeaderboardAsync();
        Assert.True(board.Count >= 2);
        for (var i = 0; i < board.Count - 1; i++)
        {
            Assert.True(board[i].PortfolioTotal >= board[i + 1].PortfolioTotal);
        }
    }
}
