using MarketService.Application.Abstractions;

namespace MarketService.Api.Endpoints;

public static class TradersLeaderboardEndpoints
{
    public static RouteGroupBuilder MapTradersLeaderboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/traders").RequireAuthorization();
        group.MapGet("/leaderboard", GetLeaderboardAsync);
        return group;
    }

    private static async Task<IResult> GetLeaderboardAsync(
        IMarketPetStore store,
        CancellationToken cancellationToken)
    {
        var rows = await store.GetLeaderboardAsync(cancellationToken).ConfigureAwait(false);
        var payload = rows.Select(r => new
        {
            traderId = r.TraderId,
            displayName = r.DisplayName,
            portfolioTotal = r.PortfolioTotal,
            rank = r.Rank
        });
        return Results.Ok(payload);
    }
}
