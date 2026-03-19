using TradeService.Application.Abstractions;
using TradeService.Domain.Entities;
using Trading.Contracts.Events;
using Trading.Messaging;

namespace TradeService.Application.Consumers;

public sealed class OrderMatchedConsumer : IIntegrationEventHandler<OrderMatched>
{
    private readonly ITradeDataStore _store;
    private readonly ITradeEventPublisher _publisher;

    public OrderMatchedConsumer(ITradeDataStore store, ITradeEventPublisher publisher)
    {
        _store = store;
        _publisher = publisher;
    }

    public Task HandleAsync(OrderMatched message, CancellationToken cancellationToken = default)
    {
        return ConsumeAsync(message, cancellationToken);
    }

    public async Task<TradeRecorded> ConsumeAsync(OrderMatched @event, CancellationToken cancellationToken = default)
    {
        var existingTrade = await _store.GetTradeByIdAsync(@event.MatchId, cancellationToken).ConfigureAwait(false);
        if (existingTrade is not null)
        {
            return MapRecorded(existingTrade);
        }

        var trade = new Trade
        {
            TradeId = @event.MatchId,
            BuyOrderId = @event.BuyOrderId,
            SellOrderId = @event.SellOrderId,
            BuyerUserId = @event.BuyerUserId,
            SellerUserId = @event.SellerUserId,
            ItemId = @event.ItemId,
            Symbol = @event.Symbol,
            Price = @event.Price,
            Quantity = @event.Quantity,
            ExecutedAtUtc = @event.MatchedAtUtc,
            CorrelationId = @event.CorrelationId
        };

        _store.AddTrade(trade);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var recorded = MapRecorded(trade);
        await _publisher.PublishAsync(recorded, cancellationToken).ConfigureAwait(false);
        return recorded;
    }

    private static TradeRecorded MapRecorded(Trade trade)
    {
        return new TradeRecorded(
            Guid.NewGuid(),
            trade.ExecutedAtUtc,
            trade.CorrelationId,
            "trade-service",
            trade.TradeId,
            trade.BuyOrderId,
            trade.SellOrderId,
            trade.BuyerUserId,
            trade.SellerUserId,
            trade.ItemId,
            trade.Symbol,
            trade.Price,
            trade.Quantity,
            trade.ExecutedAtUtc);
    }
}
