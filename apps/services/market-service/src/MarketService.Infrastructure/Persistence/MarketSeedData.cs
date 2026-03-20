using MarketService.Domain.Entities;

namespace MarketService.Infrastructure.Persistence;

internal static class MarketSeedData
{
    public static readonly Guid AbcItemId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid XyzItemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly DateTimeOffset CreatedAtUtc = new(2026, 3, 16, 0, 0, 0, TimeSpan.Zero);
    public const decimal AutoProvisionedCashAvailable = 100000m;

    public static readonly Item[] Items =
    [
        new Item
        {
            ItemId = AbcItemId,
            Symbol = "ABC",
            Name = "Acme Beverage Co",
            Category = "Equity",
            ReferencePrice = 100m,
            IsTradable = true,
            CreatedAtUtc = CreatedAtUtc
        },
        new Item
        {
            ItemId = XyzItemId,
            Symbol = "XYZ",
            Name = "Xylophone Yield Zone",
            Category = "Equity",
            ReferencePrice = 80m,
            IsTradable = true,
            CreatedAtUtc = CreatedAtUtc
        }
    ];

    public static readonly DemoAccountRecord[] Accounts =
    [
        new DemoAccountRecord
        {
            UserId = "user-1",
            DisplayName = "Buyer One",
            Email = "user1@example.com"
        },
        new DemoAccountRecord
        {
            UserId = "user-2",
            DisplayName = "Seller Two",
            Email = "user2@example.com"
        },
        new DemoAccountRecord
        {
            UserId = "user-3",
            DisplayName = "Observer Three",
            Email = "user3@example.com"
        }
    ];

    /// <summary>Spendable cash for seeded demo users; stored on PetTraders (ExternalUserId = UserId).</summary>
    public static readonly (string UserId, string DisplayName, decimal AvailableCash)[] SeededUserWallets =
    [
        ("user-1", "Buyer One", 250000m),
        ("user-2", "Seller Two", 150000m),
        ("user-3", "Observer Three", 50000m)
    ];
}
