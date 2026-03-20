using MarketService.Domain.Entities;

namespace MarketService.Application.Abstractions;

public sealed class DemoAccount
{
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public decimal CashAvailable { get; set; }
    public Dictionary<string, int> Holdings { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface IMarketDataStore
{
    Task<DemoAccount?> GetAccountAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DemoAccount>> GetAccountsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    Task<DemoAccount> EnsureDemoAccountAsync(string userId, string? displayName, string? email, CancellationToken cancellationToken = default);
    Task SaveAccountsAsync(IEnumerable<DemoAccount> accounts, CancellationToken cancellationToken = default);
    Task<Item?> GetItemBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task<List<Item>> GetItemsAsync(CancellationToken cancellationToken = default);
    Task<List<Order>> GetOpenOrdersAsync(string symbol, CancellationToken cancellationToken = default);
    Task<DateTimeOffset> GetLastUpdatedAtUtcAsync(string symbol, CancellationToken cancellationToken = default);
    void AddOrder(Order order);
    void AddAudits(IEnumerable<OrderMatchAudit> audits);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ILifecycleEventPublisher
{
    Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default) where TEvent : class, Trading.Contracts.Events.ITradingEvent;
}
