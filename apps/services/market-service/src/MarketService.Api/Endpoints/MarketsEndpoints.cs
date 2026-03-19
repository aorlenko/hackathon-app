using MarketService.Application.Markets;

namespace MarketService.Api.Endpoints;

public static class MarketsEndpoints
{
    public static RouteGroupBuilder MapMarketsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/markets");
        group.MapGet(string.Empty, GetMarkets);
        group.MapGet("/{symbol}/order-book", GetOrderBook);
        return group;
    }

    public static async Task<IResult> GetMarkets(GetMarketsHandler handler, CancellationToken cancellationToken)
    {
        return Results.Ok(await handler.HandleAsync(cancellationToken).ConfigureAwait(false));
    }

    public static async Task<IResult> GetOrderBook(string symbol, GetOrderBookHandler handler, CancellationToken cancellationToken)
    {
        return Results.Ok(await handler.HandleAsync(symbol, cancellationToken).ConfigureAwait(false));
    }
}
