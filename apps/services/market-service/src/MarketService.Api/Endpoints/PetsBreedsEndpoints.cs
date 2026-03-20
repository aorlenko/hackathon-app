using MarketService.Application.Abstractions;

namespace MarketService.Api.Endpoints;

public static class PetsBreedsEndpoints
{
    public static RouteGroupBuilder MapPetsBreedsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pets").RequireAuthorization();
        group.MapGet("/breeds", GetBreedsAsync);
        return group;
    }

    private static async Task<IResult> GetBreedsAsync(
        IMarketPetStore store,
        CancellationToken cancellationToken)
    {
        var rows = await store.GetBreedsWithSupplyAsync(cancellationToken).ConfigureAwait(false);
        var payload = rows.Select(b => new
        {
            id = b.Id,
            name = b.Name,
            category = b.Category,
            lifespanYears = b.LifespanYears,
            baselineDesirability = b.BaselineDesirability,
            maintenanceCost = b.MaintenanceCost,
            retailPrice = b.RetailPrice,
            remainingSupply = b.RemainingSupply
        });
        return Results.Ok(payload);
    }
}
