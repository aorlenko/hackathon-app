using System.Security.Claims;
using MarketService.Application.Abstractions;
using MarketService.Application.Authorization;

namespace MarketService.Api.Endpoints;

public static class TradersSnapshotEndpoints
{
    public static RouteGroupBuilder MapTradersSnapshotEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/traders").RequireAuthorization();
        group.MapGet("/me/snapshot", GetMySnapshotAsync);
        group.MapGet("/{traderId:guid}/snapshot", GetSnapshotAsync);
        return group;
    }

    private static async Task<IResult> GetMySnapshotAsync(
        HttpContext httpContext,
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
        var snapshot = await store.GetTraderSnapshotAsync(traderId, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToSnapshotDto(snapshot));
    }

    private static async Task<IResult> GetSnapshotAsync(
        HttpContext httpContext,
        Guid traderId,
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

        var snapshot = await store.GetTraderSnapshotAsync(traderId, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToSnapshotDto(snapshot));
    }

    private static object ToSnapshotDto(TraderSnapshotRow snapshot)
    {
        var pets = snapshot.Pets.Select(p => new
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
        var myBids = snapshot.MyBids.Select(b => new
        {
            bidId = b.BidId,
            listingId = b.ListingId,
            petId = b.PetId,
            amount = b.Amount,
            status = b.Status
        });
        return new
        {
            traderId = snapshot.TraderId,
            displayName = snapshot.DisplayName,
            availableCash = snapshot.AvailableCash,
            lockedCash = snapshot.LockedCash,
            portfolioTotal = snapshot.PortfolioTotal,
            pets,
            myBids
        };
    }
}
