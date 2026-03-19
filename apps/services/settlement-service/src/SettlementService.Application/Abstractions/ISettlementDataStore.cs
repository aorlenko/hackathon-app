using SettlementService.Domain.Entities;
using Trading.Contracts.Events;

namespace SettlementService.Application.Abstractions;

public interface ISettlementDataStore
{
    void AddSettlement(Settlement settlement);
    Task<Settlement?> GetSettlementByTradeIdAsync(Guid tradeId, CancellationToken cancellationToken = default);
    Task<List<Settlement>> GetUserSettlementsAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ISettlementEventPublisher
{
    Task PublishStartedAsync(SettlementStarted message, CancellationToken cancellationToken = default);
    Task PublishCompletedAsync(SettlementCompleted message, CancellationToken cancellationToken = default);
}
