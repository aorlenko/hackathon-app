using MarketService.Application.Pets;
using MarketService.Domain.Entities;
using MarketService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketService.Infrastructure.Seeding;

public static class SeedDataRunner
{
    public static async Task SeedAsync(MarketDbContext store, CancellationToken cancellationToken = default)
    {
        if (await store.Accounts.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        store.Accounts.AddRange(MarketSeedData.Accounts.Select(account => new DemoAccountRecord
        {
            UserId = account.UserId,
            DisplayName = account.DisplayName,
            Email = account.Email,
            CashAvailable = account.CashAvailable
        }));

        store.Holdings.AddRange(MarketSeedData.Holdings.Select(holding => new DemoHoldingRecord
        {
            UserId = holding.UserId,
            Symbol = holding.Symbol,
            Quantity = holding.Quantity
        }));

        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Legacy ABC/XYZ catalog for in-memory order-book contract tests only.</summary>
    public static async Task EnsureLegacyEquityCatalogAsync(
        MarketDbContext store,
        CancellationToken cancellationToken = default)
    {
        if (await store.Items.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        store.Items.AddRange(MarketSeedData.Items);
        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task EnsureTradingPetsSeedAsync(
        MarketDbContext store,
        TradingPetsOptions options,
        CancellationToken cancellationToken = default)
    {
        if (await store.Breeds.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var breeds = MarketPetSeedData.CreateBreeds(options.DefaultSupplyPerBreed);
        store.Breeds.AddRange(breeds);

        for (var i = 1; i <= options.TraderSeedCount; i++)
        {
            store.Traders.Add(new Trader
            {
                Id = MarketPetSeedData.TraderGuid(i),
                DisplayName = $"Trader {i}",
                ExternalUserId = null,
                AvailableCash = options.InitialTraderCash,
                LockedCash = 0,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
