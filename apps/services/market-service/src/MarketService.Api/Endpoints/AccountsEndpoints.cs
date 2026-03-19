using MarketService.Application.Abstractions;
using Trading.Contracts.Http;

namespace MarketService.Api.Endpoints;

public static class AccountsEndpoints
{
    public static RouteGroupBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").RequireAuthorization();
        group.MapPost("/me/bootstrap", BootstrapAccount);
        return group;
    }

    public static async Task<IResult> BootstrapAccount(
        HttpContext context,
        BootstrapDemoAccountRequest request,
        IMarketDataStore store,
        CancellationToken cancellationToken)
    {
        var principalUser = CurrentUserProfileReader.Read(context.User);
        if (principalUser is null)
        {
            return Results.Unauthorized();
        }

        var user = CurrentUserProfileReader.FromBootstrap(
            principalUser.UserId,
            request.DisplayName ?? principalUser.DisplayName,
            request.Email ?? principalUser.Email);

        var account = await store.EnsureDemoAccountAsync(user.UserId, user.DisplayName, user.Email, cancellationToken)
            .ConfigureAwait(false);

        var response = new DemoAccountDto(
            account.UserId,
            account.DisplayName,
            account.Email,
            account.CashAvailable,
            account.Holdings
                .OrderBy(holding => holding.Key, StringComparer.OrdinalIgnoreCase)
                .Select(holding => new DemoHoldingDto(holding.Key, holding.Value))
                .ToList());

        return Results.Ok(response);
    }
}
