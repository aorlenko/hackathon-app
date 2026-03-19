using MarketService.Application.Abstractions;
using Trading.Contracts.Http;

namespace MarketService.Application.Orders;

public sealed class OrderValidationPolicy
{
    public async Task<(bool IsValid, string? ErrorCode, string? Message)> ValidateAsync(IMarketDataStore store, string userId, PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (false, "unauthorized", "Authenticated user is required.");
        }

        if (request.Price <= 0 || request.Quantity <= 0)
        {
            return (false, "invalid_order", "Price and quantity must be positive.");
        }

        var account = await store.GetAccountAsync(userId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return (false, "account_not_found", "No demo account exists for the user.");
        }

        var item = await store.GetItemBySymbolAsync(request.ItemSymbol, cancellationToken).ConfigureAwait(false);
        if (item is null || !item.IsTradable)
        {
            return (false, "item_not_tradable", "The requested item is not tradable.");
        }

        if (request.Side == OrderSideDto.BUY)
        {
            var cost = request.Price * request.Quantity;
            if (account.CashAvailable < cost)
            {
                return (false, "insufficient_cash", "Insufficient simulated cash balance.");
            }
        }
        else
        {
            account.Holdings.TryGetValue(item.Symbol, out var holding);
            if (holding < request.Quantity)
            {
                return (false, "insufficient_holdings", "Insufficient simulated holdings.");
            }
        }

        return (true, null, null);
    }
}
