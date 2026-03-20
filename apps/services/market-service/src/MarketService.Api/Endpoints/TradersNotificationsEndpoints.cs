using MarketService.Application.Abstractions;
using MarketService.Application.Authorization;

namespace MarketService.Api.Endpoints;

public static class TradersNotificationsEndpoints
{
    public static RouteGroupBuilder MapTradersNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/traders").RequireAuthorization();
        group.MapGet("/{traderId:guid}/notifications", GetNotificationsAsync);
        return group;
    }

    private static async Task<IResult> GetNotificationsAsync(
        HttpContext httpContext,
        Guid traderId,
        int? limit,
        IMarketPetStore store,
        TraderAuthorizationHelper authorization,
        CancellationToken cancellationToken)
    {
        var guard = await TradingPetsAuth.RequireTraderAsync(httpContext, traderId, authorization, cancellationToken)
            .ConfigureAwait(false);
        if (guard is not null)
        {
            return guard;
        }

        var rows = await store.GetNotificationsAsync(traderId, limit ?? 50, cancellationToken).ConfigureAwait(false);
        var payload = rows.Select(n => new
        {
            id = n.Id,
            type = n.Type,
            createdAt = n.CreatedAt,
            petId = n.PetId,
            petName = n.PetName,
            amount = n.Amount,
            counterpartyTraderId = n.CounterpartyTraderId,
            counterpartyDisplayName = n.CounterpartyDisplayName
        });
        return Results.Ok(payload);
    }
}
