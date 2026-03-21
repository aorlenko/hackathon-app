using Microsoft.EntityFrameworkCore;
using SettlementService.Application.Abstractions;
using SettlementService.Domain.Entities;

namespace SettlementService.Infrastructure.Persistence;

public sealed class SettlementDbContext : DbContext, ISettlementDataStore
{
    public SettlementDbContext(DbContextOptions<SettlementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Settlement> Settlements => Set<Settlement>();

    public void AddSettlement(Settlement settlement) => Settlements.Add(settlement);

    public Task<Settlement?> GetSettlementByTradeIdAsync(Guid tradeId, CancellationToken cancellationToken = default)
    {
        return Settlements.FirstOrDefaultAsync(x => x.TradeId == tradeId, cancellationToken);
    }

    public Task<List<Settlement>> GetUserSettlementsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Settlements
            .Where(x => x.BuyerUserId == userId || x.SellerUserId == userId)
            .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Settlement>(entity =>
        {
            entity.ToTable("Settlements");
            entity.HasKey(x => x.SettlementId);
            entity.HasIndex(x => x.TradeId).IsUnique();
            entity.Property(x => x.BuyerUserId).HasMaxLength(128);
            entity.Property(x => x.SellerUserId).HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(x => x.FailureReason).HasMaxLength(512);
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
