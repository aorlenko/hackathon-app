using SettlementService.Application.Queries;

namespace SettlementService.Api.Endpoints;

public static class SettlementsEndpoints
{
    public static RouteGroupBuilder MapSettlementsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");
        group.MapGet("/settlements/{tradeId:guid}", GetSettlement);
        group.MapGet("/users/{userId}/settlements", GetUserSettlements);
        return group;
    }

    public static async Task<IResult> GetSettlement(Guid tradeId, GetSettlementByTradeQuery query, CancellationToken cancellationToken)
    {
        var settlement = await query.ExecuteAsync(tradeId, cancellationToken).ConfigureAwait(false);
        return settlement is null ? Results.NotFound() : Results.Ok(settlement);
    }

    public static async Task<IResult> GetUserSettlements(string userId, GetUserSettlementsQuery query, CancellationToken cancellationToken)
    {
        return Results.Ok(await query.ExecuteAsync(userId, cancellationToken).ConfigureAwait(false));
    }
}
