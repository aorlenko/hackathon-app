using MarketService.Application.Abstractions;
using MarketService.Application.Accounts;
using MarketService.Application.Pets;
using MarketService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarketService.Infrastructure.Persistence;

public sealed class MarketDbContext : DbContext, IMarketDataStore
{
    private readonly TradingPetsOptions _tradingPets;

    public MarketDbContext(
        DbContextOptions<MarketDbContext> options,
        IOptions<TradingPetsOptions> tradingPetsOptions)
        : base(options)
    {
        _tradingPets = tradingPetsOptions.Value;
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderMatchAudit> Audits => Set<OrderMatchAudit>();
    public DbSet<DemoAccountRecord> Accounts => Set<DemoAccountRecord>();
    public DbSet<Trader> Traders => Set<Trader>();
    public DbSet<Breed> Breeds => Set<Breed>();
    public DbSet<Supply> Supplies => Set<Supply>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Notification> Notifications => Set<Notification>();

    public void AddOrder(Order order) => Orders.Add(order);

    public void AddAudits(IEnumerable<OrderMatchAudit> audits) => Audits.AddRange(audits);

    public async Task<DemoAccount?> GetAccountAsync(string userId, CancellationToken cancellationToken = default)
    {
        var account = await Accounts
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return null;
        }

        var spendable = await GetTraderSpendableCashAsync(userId, cancellationToken).ConfigureAwait(false);
        return new DemoAccount
        {
            UserId = account.UserId,
            DisplayName = account.DisplayName,
            Email = MarketUserIdentityDefaults.NormalizeEmail(account.Email, account.UserId),
            CashAvailable = spendable
        };
    }

    public async Task<IReadOnlyList<DemoAccount>> GetAccountsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var normalizedUserIds = userIds
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Select(userId => userId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedUserIds.Count == 0)
        {
            return [];
        }

        var accounts = await Accounts
            .Where(account => normalizedUserIds.Contains(account.UserId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var cashByUser = await Traders.AsNoTracking()
            .Where(t => t.ExternalUserId != null && normalizedUserIds.Contains(t.ExternalUserId))
            .ToDictionaryAsync(t => t.ExternalUserId!, t => t.AvailableCash, StringComparer.Ordinal, cancellationToken)
            .ConfigureAwait(false);

        return accounts
            .Select(account => new DemoAccount
            {
                UserId = account.UserId,
                DisplayName = account.DisplayName,
                Email = MarketUserIdentityDefaults.NormalizeEmail(account.Email, account.UserId),
                CashAvailable = cashByUser.GetValueOrDefault(account.UserId, 0m)
            })
            .ToList();
    }

    public async Task<DemoAccount> EnsureDemoAccountAsync(string userId, string? displayName, string? email, CancellationToken cancellationToken = default)
    {
        var normalizedDisplayName = MarketUserIdentityDefaults.NormalizeDisplayName(displayName, userId);
        var emailForStorage = MarketUserIdentityDefaults.NormalizeEmail(email, userId);

        var existing = await Accounts
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            existing = new DemoAccountRecord
            {
                UserId = userId,
                DisplayName = normalizedDisplayName,
                Email = emailForStorage
            };

            Accounts.Add(existing);
            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await EnsureUserWalletTraderAsync(
                    userId,
                    normalizedDisplayName,
                    _tradingPets.InitialTraderCash,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var changed = false;
            if (!string.IsNullOrWhiteSpace(normalizedDisplayName) && !string.Equals(existing.DisplayName, normalizedDisplayName, StringComparison.Ordinal))
            {
                existing.DisplayName = normalizedDisplayName;
                changed = true;
            }

            // Only change email when the caller supplied one. Otherwise a JWT without an email claim
            // would synthesize trader-*@demo.local and overwrite a real address from bootstrap.
            if (!string.IsNullOrWhiteSpace(email))
            {
                var nextEmail = MarketUserIdentityDefaults.NormalizeEmail(email, userId);
                if (!string.Equals(existing.Email, nextEmail, StringComparison.OrdinalIgnoreCase))
                {
                    existing.Email = nextEmail;
                    changed = true;
                }
            }

            if (changed)
            {
                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            await EnsureUserWalletTraderAsync(
                    userId,
                    existing.DisplayName,
                    _tradingPets.InitialTraderCash,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return await GetAccountAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Could not provision demo account for {userId}.");
    }

    public async Task<decimal> GetTraderSpendableCashAsync(string userId, CancellationToken cancellationToken = default)
    {
        var row = await Traders.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ExternalUserId == userId, cancellationToken)
            .ConfigureAwait(false);
        return row?.AvailableCash ?? 0m;
    }

    public async Task AdjustTraderSpendableCashAsync(string userId, decimal delta, CancellationToken cancellationToken = default)
    {
        var row = await Traders.FirstOrDefaultAsync(t => t.ExternalUserId == userId, cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            throw new InvalidOperationException($"No wallet trader exists for user {userId}.");
        }

        var next = row.AvailableCash + delta;
        if (next < 0)
        {
            throw new InvalidOperationException($"Wallet for {userId} would be negative ({next}).");
        }

        row.AvailableCash = next;
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureUserWalletTraderAsync(
        string userId,
        string displayName,
        decimal initialCashIfNew,
        CancellationToken cancellationToken)
    {
        var row = await Traders.FirstOrDefaultAsync(t => t.ExternalUserId == userId, cancellationToken).ConfigureAwait(false);
        if (row is not null)
        {
            var next = string.IsNullOrWhiteSpace(displayName) ? row.DisplayName : displayName.Trim();
            if (next.Length > 0 && !string.Equals(row.DisplayName, next, StringComparison.Ordinal))
            {
                row.DisplayName = next;
                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return;
        }

        Traders.Add(new Trader
        {
            Id = Guid.NewGuid(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Trader" : displayName.Trim(),
            ExternalUserId = userId,
            AvailableCash = initialCashIfNew,
            LockedCash = 0
        });
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<Item?> GetItemBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return Items.FirstOrDefaultAsync(x => x.Symbol == symbol.ToUpperInvariant(), cancellationToken);
    }

    public Task<List<Item>> GetItemsAsync(CancellationToken cancellationToken = default)
    {
        return Items.OrderBy(x => x.Symbol).ToListAsync(cancellationToken);
    }

    public Task<List<Order>> GetOpenOrdersAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return Orders
            .Where(x => x.Symbol == symbol.ToUpperInvariant() && (x.Status == OrderStatus.OPEN || x.Status == OrderStatus.PARTIALLY_FILLED))
            .OrderBy(x => x.AcceptedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<DateTimeOffset> GetLastUpdatedAtUtcAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var lastUpdated = await Orders
            .Where(x => x.Symbol == symbol.ToUpperInvariant())
            .OrderByDescending(x => x.LastUpdatedAtUtc)
            .Select(x => (DateTimeOffset?)x.LastUpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return lastUpdated ?? DateTimeOffset.UtcNow;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Items");
            entity.HasKey(x => x.ItemId);
            entity.Property(x => x.Symbol).HasMaxLength(32);
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.Category).HasMaxLength(128);
            entity.Property(x => x.ReferencePrice).HasColumnType("decimal(18,4)");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.Symbol).IsUnique();
            entity.HasData(MarketSeedData.Items);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(x => x.OrderId);
            entity.Property(x => x.UserId).HasMaxLength(128);
            entity.Property(x => x.Symbol).HasMaxLength(32);
            entity.Property(x => x.Side).HasConversion<string>().HasMaxLength(16);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.Price).HasColumnType("decimal(18,4)");
            entity.Property(x => x.RejectionReason).HasMaxLength(512);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<OrderMatchAudit>(entity =>
        {
            entity.ToTable("OrderMatchAudits");
            entity.HasKey(x => x.AuditId);
            entity.Property(x => x.MatchPrice).HasColumnType("decimal(18,4)");
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<DemoAccountRecord>(entity =>
        {
            entity.ToTable("DemoAccounts");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasMaxLength(128);
            entity.Property(x => x.DisplayName).HasMaxLength(256);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasData(MarketSeedData.Accounts);
        });

        modelBuilder.Entity<Trader>(entity =>
        {
            entity.ToTable("PetTraders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(256);
            entity.Property(x => x.ExternalUserId).HasMaxLength(256);
            entity.Property(x => x.AvailableCash).HasColumnType("decimal(18,2)");
            entity.Property(x => x.LockedCash).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.ExternalUserId);
        });

        modelBuilder.Entity<Breed>(entity =>
        {
            entity.ToTable("PetBreeds");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.LifespanYears).HasColumnType("decimal(18,4)");
            entity.Property(x => x.MaintenanceCost).HasColumnType("decimal(18,2)");
            entity.Property(x => x.RetailPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Supply>(entity =>
        {
            entity.ToTable("PetBreedSupply");
            entity.HasKey(x => x.BreedId);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Breed)
                .WithOne(x => x.Supply)
                .HasForeignKey<Supply>(x => x.BreedId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pet>(entity =>
        {
            entity.ToTable("Pets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AgeYears).HasColumnType("decimal(18,6)");
            entity.Property(x => x.Health).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Breed)
                .WithMany(x => x.Pets)
                .HasForeignKey(x => x.BreedId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Owner)
                .WithMany(x => x.Pets)
                .HasForeignKey(x => x.OwnerTraderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.OwnerTraderId);
            entity.HasIndex(x => x.BreedId);
        });

        modelBuilder.Entity<Listing>(entity =>
        {
            entity.ToTable("PetListings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.AskingPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(x => x.Pet)
                .WithMany()
                .HasForeignKey(x => x.PetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Seller)
                .WithMany(x => x.ListingsAsSeller)
                .HasForeignKey(x => x.SellerTraderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.PetId, x.WithdrawnAt });
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.ToTable("PetListingBids");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Listing)
                .WithMany(x => x.Bids)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Buyer)
                .WithMany(x => x.BidsAsBuyer)
                .HasForeignKey(x => x.BuyerTraderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.ListingId, x.Status });
        });

        modelBuilder.Entity<Trade>(entity =>
        {
            entity.ToTable("PetTrades");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Price).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Pet)
                .WithMany()
                .HasForeignKey(x => x.PetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Listing)
                .WithMany(x => x.Trades)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Buyer)
                .WithMany()
                .HasForeignKey(x => x.BuyerTraderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Seller)
                .WithMany()
                .HasForeignKey(x => x.SellerTraderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.ExecutedAt);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("PetNotifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.PetName).HasMaxLength(256);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CounterpartyDisplayName).HasMaxLength(256);
            entity.Property(x => x.Correlation).HasMaxLength(128);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Trader)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.TraderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.TraderId, x.CreatedAt });
        });
    }
}

public sealed class DemoAccountRecord
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
