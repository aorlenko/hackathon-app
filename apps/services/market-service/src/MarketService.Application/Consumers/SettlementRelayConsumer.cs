using MarketService.Application.Realtime;
using Trading.Contracts.Events;
using Trading.Contracts.Http;
using Trading.Messaging;

namespace MarketService.Application.Consumers;

public sealed class SettlementRelayConsumer :
    IIntegrationEventHandler<SettlementStarted>,
    IIntegrationEventHandler<SettlementCompleted>
{
    private readonly IMarketRealtimeNotifier _notifier;

    public SettlementRelayConsumer(IMarketRealtimeNotifier notifier)
    {
        _notifier = notifier;
    }

    Task IIntegrationEventHandler<SettlementStarted>.HandleAsync(SettlementStarted @event, CancellationToken cancellationToken)
    {
        var payload = new SettlementUpdatedRealtimeDto(@event.TradeId, @event.Status, @event.StartedAtUtc, null);
        return _notifier.NotifySettlementUpdatedAsync(@event.BuyerUserId, @event.SellerUserId, payload, cancellationToken);
    }

    Task IIntegrationEventHandler<SettlementCompleted>.HandleAsync(SettlementCompleted @event, CancellationToken cancellationToken)
    {
        var payload = new SettlementUpdatedRealtimeDto(@event.TradeId, @event.Status, @event.CompletedAtUtc, @event.FailureReason);
        return _notifier.NotifySettlementUpdatedAsync(@event.BuyerUserId, @event.SellerUserId, payload, cancellationToken);
    }
}
