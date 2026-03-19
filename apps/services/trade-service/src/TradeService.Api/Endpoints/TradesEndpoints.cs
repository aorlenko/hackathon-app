using TradeService.Application.Queries;

namespace TradeService.Api.Endpoints;

public static class TradesEndpoints
{
    public static RouteGroupBuilder MapTradesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");
        group.MapGet("/trades", GetTrades);
        group.MapGet("/users/{userId}/trades", GetUserTrades);
        return group;
    }

    public static async Task<IResult> GetTrades(string symbol, int? limit, GetRecentTradesQuery query, CancellationToken cancellationToken)
    {
        return Results.Ok(await query.ExecuteAsync(symbol, limit ?? 50, cancellationToken).ConfigureAwait(false));
    }

    public static async Task<IResult> GetUserTrades(string userId, GetUserTradesQuery query, CancellationToken cancellationToken)
    {
        return Results.Ok(await query.ExecuteAsync(userId, cancellationToken).ConfigureAwait(false));
    }
}
