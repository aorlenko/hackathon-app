using MarketService.Domain.Entities;
using MarketService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MarketService.Infrastructure.Seeding;

public static class SeedDataRunner
{
    public static async Task SeedAsync(MarketDbContext store, CancellationToken cancellationToken = default)
    {
        if (await store.Items.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        store.Items.AddRange(
        [
            new Item
            {
                ItemId = MarketSeedData.AbcItemId,
                Symbol = "ABC",
                Name = "Acme Beverage Co",
                Category = "Equity",
                ReferencePrice = 100m,
                IsTradable = true,
                CreatedAtUtc = MarketSeedData.CreatedAtUtc
            },
            new Item
            {
                ItemId = MarketSeedData.XyzItemId,
                Symbol = "XYZ",
                Name = "Xylophone Yield Zone",
                Category = "Equity",
                ReferencePrice = 80m,
                IsTradable = true,
                CreatedAtUtc = MarketSeedData.CreatedAtUtc
            }
        ]);

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
}
