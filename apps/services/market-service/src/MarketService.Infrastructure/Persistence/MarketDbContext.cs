using MarketService.Application.Abstractions;
using MarketService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketService.Infrastructure.Persistence;

public sealed class MarketDbContext : DbContext, IMarketDataStore
{
    public MarketDbContext(DbContextOptions<MarketDbContext> options)
        : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderMatchAudit> Audits => Set<OrderMatchAudit>();
    public DbSet<DemoAccountRecord> Accounts => Set<DemoAccountRecord>();
    public DbSet<DemoHoldingRecord> Holdings => Set<DemoHoldingRecord>();

    public void AddOrder(Order order) => Orders.Add(order);

    public void AddAudits(IEnumerable<OrderMatchAudit> audits) => Audits.AddRange(audits);

    public async Task<DemoAccount?> GetAccountAsync(string userId, CancellationToken cancellationToken = default)
    {
        var account = await Accounts
            .Include(x => x.Holdings)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return null;
        }

        var result = new DemoAccount
        {
            UserId = account.UserId,
            DisplayName = account.DisplayName,
            Email = account.Email,
            CashAvailable = account.CashAvailable
        };

        foreach (var holding in account.Holdings)
        {
            result.Holdings[holding.Symbol] = holding.Quantity;
        }

        return result;
    }

    public async Task<DemoAccount> EnsureDemoAccountAsync(string userId, string? displayName, string? email, CancellationToken cancellationToken = default)
    {
        var normalizedDisplayName = string.IsNullOrWhiteSpace(displayName) ? userId : displayName.Trim();
        var normalizedEmail = email?.Trim() ?? string.Empty;

        var existing = await Accounts
            .Include(x => x.Holdings)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            existing = new DemoAccountRecord
            {
                UserId = userId,
                DisplayName = normalizedDisplayName,
                Email = normalizedEmail,
                CashAvailable = MarketSeedData.AutoProvisionedCashAvailable,
                Holdings = MarketSeedData.AutoProvisionedHoldings
                    .Select(holding => new DemoHoldingRecord
                    {
                        UserId = userId,
                        Symbol = holding.Symbol,
                        Quantity = holding.Quantity
                    })
                    .ToList()
            };

            Accounts.Add(existing);
            await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var changed = false;
            if (!string.IsNullOrWhiteSpace(normalizedDisplayName) && !string.Equals(existing.DisplayName, normalizedDisplayName, StringComparison.Ordinal))
            {
                existing.DisplayName = normalizedDisplayName;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace(normalizedEmail) && !string.Equals(existing.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                existing.Email = normalizedEmail;
                changed = true;
            }

            if (changed)
            {
                await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return await GetAccountAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Could not provision demo account for {userId}.");
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
        });

        modelBuilder.Entity<OrderMatchAudit>(entity =>
        {
            entity.ToTable("OrderMatchAudits");
            entity.HasKey(x => x.AuditId);
            entity.Property(x => x.MatchPrice).HasColumnType("decimal(18,4)");
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
        });

        modelBuilder.Entity<DemoAccountRecord>(entity =>
        {
            entity.ToTable("DemoAccounts");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasMaxLength(128);
            entity.Property(x => x.DisplayName).HasMaxLength(256);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.CashAvailable).HasColumnType("decimal(18,2)");
            entity.HasMany(x => x.Holdings)
                .WithOne()
                .HasForeignKey(x => x.UserId);
            entity.HasData(MarketSeedData.Accounts);
        });

        modelBuilder.Entity<DemoHoldingRecord>(entity =>
        {
            entity.ToTable("DemoHoldings");
            entity.HasKey(x => new { x.UserId, x.Symbol });
            entity.Property(x => x.UserId).HasMaxLength(128);
            entity.Property(x => x.Symbol).HasMaxLength(32);
            entity.HasData(MarketSeedData.Holdings);
        });
    }
}

public sealed class DemoAccountRecord
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal CashAvailable { get; set; }
    public List<DemoHoldingRecord> Holdings { get; set; } = [];
}

public sealed class DemoHoldingRecord
{
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
