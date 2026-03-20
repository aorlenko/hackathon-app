using System.Security.Claims;
using MarketService.Application.Abstractions;
using MarketService.Application.Pets.Terminal;

namespace MarketService.Api.Endpoints;

public static class PetsTerminalEndpoints
{
    public static RouteGroupBuilder MapPetsTerminalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pets/terminal").RequireAuthorization();
        group.MapGet("/markets", GetTerminalMarketsAsync);
        group.MapGet("/workspace/{marketEntryId:guid}", GetTerminalWorkspaceAsync);
        return group;
    }

    private static async Task<IResult> GetTerminalMarketsAsync(
        GetTerminalMarketsHandler handler,
        CancellationToken cancellationToken)
    {
        var rows = await handler.HandleAsync(cancellationToken).ConfigureAwait(false);
        var payload = rows.Select(r => new
        {
            marketEntryId = r.MarketEntryId,
            displayName = r.DisplayName,
            currentSupply = r.CurrentSupply,
            latestTradePrice = r.LatestTradePrice,
            bestBidPrice = r.BestBidPrice,
            bestAskPrice = r.BestAskPrice,
            trendDirection = TrendToJson(r.TrendDirection),
            lastTradeAt = r.LastTradeAt
        });
        return Results.Ok(payload);
    }

    private static async Task<IResult> GetTerminalWorkspaceAsync(
        HttpContext httpContext,
        Guid marketEntryId,
        GetTerminalWorkspaceHandler handler,
        IMarketPetStore store,
        CancellationToken cancellationToken)
    {
        var sub = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(sub))
        {
            return Results.Unauthorized();
        }

        var displayName = httpContext.User.FindFirst("name")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.Name)?.Value
            ?? httpContext.User.FindFirst("email")?.Value
            ?? sub;

        var email = httpContext.User.FindFirst("email")?.Value;

        var traderId = await store.EnsureLinkedTraderForUserAsync(sub, displayName, email, cancellationToken)
            .ConfigureAwait(false);

        var snapshot = await handler.HandleAsync(traderId, marketEntryId, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToWorkspaceDto(snapshot));
    }

    private static object ToWorkspaceDto(TerminalWorkspaceSnapshot s)
    {
        var m = s.MarketEntry;
        var ob = s.OrderBook;
        var acct = s.AccountSummary;

        return new
        {
            marketEntry = new
            {
                marketEntryId = m.MarketEntryId,
                displayName = m.DisplayName,
                currentSupply = m.CurrentSupply,
                latestTradePrice = m.LatestTradePrice,
                bestBidPrice = m.BestBidPrice,
                bestAskPrice = m.BestAskPrice,
                trendDirection = TrendToJson(m.TrendDirection),
                lastTradeAt = m.LastTradeAt
            },
            orderBook = new
            {
                capturedAt = ob.CapturedAt,
                bids = ob.Bids.Select(l => new
                {
                    price = l.Price,
                    quantity = l.Quantity,
                    orderCount = l.OrderCount,
                    oldestOrderAt = l.OldestOrderAt
                }),
                asks = ob.Asks.Select(l => new
                {
                    price = l.Price,
                    quantity = l.Quantity,
                    orderCount = l.OrderCount,
                    oldestOrderAt = l.OldestOrderAt
                })
            },
            accountSummary = new
            {
                traderId = acct.TraderId,
                displayName = acct.DisplayName,
                availableCash = acct.AvailableCash,
                lockedCash = acct.LockedCash,
                portfolioTotal = acct.PortfolioTotal,
                ownedQuantity = acct.OwnedQuantity,
                eligibleAskQuantity = acct.EligibleAskQuantity
            },
            recentTrades = s.RecentTrades.Select(t => new
            {
                tradeId = t.TradeId,
                marketEntryId = t.MarketEntryId,
                price = t.Price,
                quantity = t.Quantity,
                executedAt = t.ExecutedAt,
                executionType = t.ExecutionType
            }),
            lastUpdatedAt = s.LastUpdatedAt
        };
    }

    private static string TrendToJson(TerminalTrendDirection d) =>
        d switch
        {
            TerminalTrendDirection.Up => "Up",
            TerminalTrendDirection.Down => "Down",
            TerminalTrendDirection.Flat => "Flat",
            TerminalTrendDirection.NoTradeData => "NoTradeData",
            _ => d.ToString()
        };
}
