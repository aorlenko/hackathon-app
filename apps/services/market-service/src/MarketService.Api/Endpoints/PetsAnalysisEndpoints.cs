using MarketService.Application.Abstractions;

namespace MarketService.Api.Endpoints;

public static class PetsAnalysisEndpoints
{
    public static RouteGroupBuilder MapPetsAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pets").RequireAuthorization();
        group.MapGet("/{petId:guid}/analysis", GetAnalysisAsync);
        return group;
    }

    private static async Task<IResult> GetAnalysisAsync(
        Guid petId,
        Guid? viewerTraderId,
        IMarketPetStore store,
        CancellationToken cancellationToken)
    {
        var analysis = await store.GetPetAnalysisAsync(petId, viewerTraderId, cancellationToken).ConfigureAwait(false);
        if (analysis is null)
        {
            return Results.NotFound(new { error = "not_found", message = "Pet analysis is not visible for this viewer." });
        }

        return Results.Ok(new
        {
            petId = analysis.PetId,
            breedName = analysis.BreedName,
            ageYears = analysis.AgeYears,
            health = analysis.Health,
            currentDesirability = analysis.CurrentDesirability,
            maintenanceCost = analysis.MaintenanceCost,
            intrinsicValue = analysis.IntrinsicValue,
            isExpired = analysis.IsExpired
        });
    }
}
