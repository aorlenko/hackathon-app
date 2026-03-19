using TradeService.Application.Abstractions;
using Trading.Contracts.Events;
using Trading.Messaging;

namespace TradeService.Infrastructure.Messaging;

public sealed class TradeRecordedPublisher : ITradeEventPublisher
{
    private readonly IEventBus _eventBus;

    public TradeRecordedPublisher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public Task PublishAsync(TradeRecorded message, CancellationToken cancellationToken = default)
    {
        return _eventBus.PublishAsync(message, cancellationToken);
    }
}
