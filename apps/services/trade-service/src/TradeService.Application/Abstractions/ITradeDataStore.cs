using TradeService.Domain.Entities;
using Trading.Contracts.Events;

namespace TradeService.Application.Abstractions;

public interface ITradeDataStore
{
    void AddTrade(Trade trade);
    Task<Trade?> GetTradeByIdAsync(Guid tradeId, CancellationToken cancellationToken = default);
    Task<List<Trade>> GetRecentTradesAsync(string symbol, int limit, CancellationToken cancellationToken = default);
    Task<List<Trade>> GetUserTradesAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ITradeEventPublisher
{
    Task PublishAsync(TradeRecorded message, CancellationToken cancellationToken = default);
}
