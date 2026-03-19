using MarketService.Application.Realtime;
using Trading.Contracts.Events;
using Trading.Messaging;

namespace MarketService.Application.Consumers;

public sealed class TradeRecordedRelayConsumer : IIntegrationEventHandler<TradeRecorded>
{
    private readonly IMarketRealtimeNotifier _notifier;

    public TradeRecordedRelayConsumer(IMarketRealtimeNotifier notifier)
    {
        _notifier = notifier;
    }

    public Task HandleAsync(TradeRecorded @event, CancellationToken cancellationToken = default)
    {
        return _notifier.NotifyTradeRecordedAsync(@event, cancellationToken);
    }
}
