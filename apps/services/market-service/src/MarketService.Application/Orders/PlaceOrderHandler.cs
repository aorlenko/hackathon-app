using MarketService.Application.Abstractions;
using MarketService.Application.Accounts;
using MarketService.Application.Matching;
using MarketService.Application.Realtime;
using MarketService.Domain.Entities;
using Trading.Contracts.Events;
using Trading.Contracts.Http;

namespace MarketService.Application.Orders;

public sealed record MarketUserProfile(string UserId, string DisplayName, string? Email);

public sealed record PlaceOrderOutcome(PlaceOrderResponse Response, IReadOnlyList<OrderMatched> Matches, string CorrelationId);

public sealed class PlaceOrderHandler
{
    private readonly IMarketDataStore _store;
    private readonly OrderValidationPolicy _validationPolicy;
    private readonly PriceTimeMatchingEngine _matchingEngine;
    private readonly ILifecycleEventPublisher _publisher;
    private readonly ApplyTradeToAccountsHandler _applyTradeToAccounts;
    private readonly IMarketRealtimeNotifier _realtimeNotifier;

    public PlaceOrderHandler(
        IMarketDataStore store,
        OrderValidationPolicy validationPolicy,
        PriceTimeMatchingEngine matchingEngine,
        ILifecycleEventPublisher publisher,
        ApplyTradeToAccountsHandler applyTradeToAccounts,
        IMarketRealtimeNotifier realtimeNotifier)
    {
        _store = store;
        _validationPolicy = validationPolicy;
        _matchingEngine = matchingEngine;
        _publisher = publisher;
        _applyTradeToAccounts = applyTradeToAccounts;
        _realtimeNotifier = realtimeNotifier;
    }

    public Task<PlaceOrderOutcome> HandleAsync(string userId, PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        return HandleAsync(new MarketUserProfile(userId, userId, null), request, cancellationToken);
    }

    public async Task<PlaceOrderOutcome> HandleAsync(MarketUserProfile user, PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(user.UserId))
        {
            throw new InvalidOperationException("Authenticated user is required.");
        }

        await _store.EnsureDemoAccountAsync(user.UserId, user.DisplayName, user.Email, cancellationToken).ConfigureAwait(false);

        var validation = await _validationPolicy.ValidateAsync(_store, user.UserId, request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.Message);
        }

        var correlationId = Guid.NewGuid().ToString("N");
        var acceptedAtUtc = DateTimeOffset.UtcNow;
        var item = await _store.GetItemBySymbolAsync(request.ItemSymbol, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The requested item is not tradable.");
        var order = Order.Create(item.ItemId, item.Symbol, user.UserId, request.Side == OrderSideDto.BUY ? OrderSide.BUY : OrderSide.SELL, request.Price, request.Quantity, acceptedAtUtc);
        _store.AddOrder(order);
        var opposingOrders = await _store.GetOpenOrdersAsync(order.Symbol, cancellationToken).ConfigureAwait(false);
        var matches = _matchingEngine.Match(
            order,
            opposingOrders.Where(x => x.Side != order.Side && x.Status is OrderStatus.OPEN or OrderStatus.PARTIALLY_FILLED).ToList(),
            correlationId,
            acceptedAtUtc);
        _store.AddAudits(matches.Select(match => match.Audit));
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var orderPlaced = new OrderPlaced(Guid.NewGuid(), acceptedAtUtc, correlationId, "market-service", order.OrderId, order.UserId, order.ItemId, order.Symbol, order.Side.ToString(), order.Price, order.Quantity, order.AcceptedAtUtc);
        await _publisher.PublishAsync(orderPlaced, cancellationToken).ConfigureAwait(false);

        var matchedEvents = matches.Select(match => new OrderMatched(
            Guid.NewGuid(),
            match.Audit.MatchedAtUtc,
            correlationId,
            "market-service",
            match.Audit.AuditId,
            match.BuyOrder.OrderId,
            match.SellOrder.OrderId,
            match.BuyOrder.UserId,
            match.SellOrder.UserId,
            order.ItemId,
            order.Symbol,
            match.Audit.MatchPrice,
            match.Audit.MatchQuantity,
            match.Audit.MatchedAtUtc)).ToList();

        foreach (var matched in matchedEvents)
        {
            var fundsUpdates = await _applyTradeToAccounts.HandleAsync(matched, cancellationToken).ConfigureAwait(false);
            await _publisher.PublishAsync(matched, cancellationToken).ConfigureAwait(false);

            foreach (var update in fundsUpdates)
            {
                await _realtimeNotifier.NotifyFundsUpdatedAsync(update, cancellationToken).ConfigureAwait(false);
            }
        }

        await _realtimeNotifier.NotifyOrderBookUpdatedAsync(order.Symbol, cancellationToken).ConfigureAwait(false);

        var response = new PlaceOrderResponse(order.OrderId, order.Status.ToString(), order.AcceptedAtUtc, order.RemainingQuantity);
        return new PlaceOrderOutcome(response, matchedEvents, correlationId);
    }
}
