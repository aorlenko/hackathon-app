using MarketService.Application.Authorization;

namespace MarketService.Api.Endpoints;

internal static class TradingPetsAuth
{
    public static async Task<IResult?> RequireTraderAsync(
        HttpContext httpContext,
        Guid traderId,
        TraderAuthorizationHelper authorization,
        CancellationToken cancellationToken)
    {
        if (CurrentUserProfileReader.Read(httpContext.User) is null)
        {
            return Results.Unauthorized();
        }

        if (!await authorization
                .CanActAsTraderAsync(httpContext.User, traderId, cancellationToken)
                .ConfigureAwait(false))
        {
            return Results.Json(
                new { error = "forbidden", message = "This trader is not available for the current user." },
                statusCode: StatusCodes.Status403Forbidden);
        }

        return null;
    }
}
