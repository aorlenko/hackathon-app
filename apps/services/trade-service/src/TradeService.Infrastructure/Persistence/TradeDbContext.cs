using Microsoft.EntityFrameworkCore;
using TradeService.Application.Abstractions;
using TradeService.Domain.Entities;

namespace TradeService.Infrastructure.Persistence;

public sealed class TradeDbContext : DbContext, ITradeDataStore
{
    public TradeDbContext(DbContextOptions<TradeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Trade> Trades => Set<Trade>();

    public void AddTrade(Trade trade) => Trades.Add(trade);

    public Task<Trade?> GetTradeByIdAsync(Guid tradeId, CancellationToken cancellationToken = default)
    {
        return Trades.FirstOrDefaultAsync(x => x.TradeId == tradeId, cancellationToken);
    }

    public Task<List<Trade>> GetRecentTradesAsync(string symbol, int limit, CancellationToken cancellationToken = default)
    {
        return Trades
            .Where(x => x.Symbol == symbol.ToUpperInvariant())
            .OrderByDescending(x => x.ExecutedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Trade>> GetUserTradesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Trades
            .Where(x => x.BuyerUserId == userId || x.SellerUserId == userId)
            .OrderByDescending(x => x.ExecutedAtUtc)
            .ToListAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trade>(entity =>
        {
            entity.ToTable("Trades");
            entity.HasKey(x => x.TradeId);
            entity.Property(x => x.BuyerUserId).HasMaxLength(128);
            entity.Property(x => x.SellerUserId).HasMaxLength(128);
            entity.Property(x => x.Symbol).HasMaxLength(32);
            entity.Property(x => x.Price).HasColumnType("decimal(18,4)");
            entity.Property(x => x.CorrelationId).HasMaxLength(128);
        });
    }
}
