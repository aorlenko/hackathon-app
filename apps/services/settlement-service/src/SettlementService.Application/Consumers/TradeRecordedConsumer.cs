using SettlementService.Application.Abstractions;
using SettlementService.Domain.Entities;
using Trading.Contracts.Events;
using Trading.Messaging;

namespace SettlementService.Application.Consumers;

public sealed class TradeRecordedConsumer : IIntegrationEventHandler<TradeRecorded>
{
    private readonly ISettlementDataStore _store;
    private readonly SettlementStateMachine _stateMachine;
    private readonly ISettlementEventPublisher _publisher;

    public TradeRecordedConsumer(ISettlementDataStore store, SettlementStateMachine stateMachine, ISettlementEventPublisher publisher)
    {
        _store = store;
        _stateMachine = stateMachine;
        _publisher = publisher;
    }

    public Task HandleAsync(TradeRecorded message, CancellationToken cancellationToken = default)
    {
        return ConsumeAsync(message, false, cancellationToken);
    }

    public async Task<Settlement> ConsumeAsync(TradeRecorded @event, bool simulateFailure = false, CancellationToken cancellationToken = default)
    {
        var existingSettlement = await _store.GetSettlementByTradeIdAsync(@event.TradeId, cancellationToken).ConfigureAwait(false);
        if (existingSettlement is not null)
        {
            return existingSettlement;
        }

        var settlement = new Settlement
        {
            SettlementId = Guid.NewGuid(),
            TradeId = @event.TradeId,
            BuyerUserId = @event.BuyerUserId,
            SellerUserId = @event.SellerUserId,
            Status = SettlementStatus.PENDING,
            StartedAtUtc = @event.ExecutedAtUtc,
            CorrelationId = @event.CorrelationId
        };

        _store.AddSettlement(settlement);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var transition = _stateMachine.Progress(settlement, simulateFailure);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _publisher.PublishStartedAsync(transition.Started, cancellationToken).ConfigureAwait(false);
        await _publisher.PublishCompletedAsync(transition.Completed, cancellationToken).ConfigureAwait(false);
        return settlement;
    }
}
