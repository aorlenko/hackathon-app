using MarketService.Application.Authorization;
using MarketService.Application.Pets;
using MarketService.Application.Realtime;

namespace MarketService.Api.Endpoints;

public static class PetsPurchaseEndpoints
{
    public static RouteGroupBuilder MapPetsPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pets").RequireAuthorization();
        group.MapPost("/purchase", PurchaseAsync);
        return group;
    }

    private static async Task<IResult> PurchaseAsync(
        HttpContext httpContext,
        PurchasePetsRequest body,
        PurchasePetsHandler handler,
        TraderAuthorizationHelper authorization,
        ITradingPetsRealtimePublisher realtime,
        CancellationToken cancellationToken)
    {
        var guard = await TradingPetsAuth.RequireTraderAsync(httpContext, body.TraderId, authorization, cancellationToken)
            .ConfigureAwait(false);
        if (guard is not null)
        {
            return guard;
        }

        var result = await handler
            .HandleAsync(body.TraderId, body.BreedId, body.Quantity, cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
        {
            return Results.BadRequest(new
            {
                error = "purchase_failed",
                message = "Insufficient cash or supply, invalid breed, or invalid quantity."
            });
        }

        await realtime.NotifyTraderSnapshotRefreshAsync(body.TraderId, cancellationToken).ConfigureAwait(false);
        await realtime.NotifyMarketListingsRefreshAsync(cancellationToken).ConfigureAwait(false);
        await realtime.NotifyLeaderboardRefreshAsync(cancellationToken).ConfigureAwait(false);

        var pets = result.Pets.Select(p => new
        {
            id = p.Id,
            breedName = p.BreedName,
            ageYears = p.AgeYears,
            health = p.Health,
            currentDesirability = p.CurrentDesirability,
            intrinsicValue = p.IntrinsicValue,
            isExpired = p.IsExpired,
            maintenanceCost = p.MaintenanceCost
        });
        return Results.Ok(new { pets, availableCash = result.AvailableCash });
    }

    public sealed record PurchasePetsRequest(Guid TraderId, Guid BreedId, int Quantity);
}
