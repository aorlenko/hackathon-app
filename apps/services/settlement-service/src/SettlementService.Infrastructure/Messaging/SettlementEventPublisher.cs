using SettlementService.Application.Abstractions;
using Trading.Contracts.Events;
using Trading.Messaging;

namespace SettlementService.Infrastructure.Messaging;

public sealed class SettlementEventPublisher : ISettlementEventPublisher
{
    private readonly IEventBus _eventBus;

    public SettlementEventPublisher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task PublishStartedAsync(SettlementStarted message, CancellationToken cancellationToken = default)
    {
        return _eventBus.PublishAsync(message, cancellationToken);
    }

    public Task PublishCompletedAsync(SettlementCompleted message, CancellationToken cancellationToken = default)
    {
        return _eventBus.PublishAsync(message, cancellationToken);
    }
}
