using MarketService.Application.Abstractions;
using MarketService.Application.Accounts;
using Trading.Contracts.Http;

namespace MarketService.Api.Endpoints;

public static class AccountsEndpoints
{
    public static RouteGroupBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").RequireAuthorization();
        group.MapPost("/me/bootstrap", BootstrapAccount);
        group.MapGet("/me", GetCurrentAccount);
        group.MapPost("/resolve", ResolveAccounts);
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

        var response = ToDemoAccountDto(account);

        return Results.Ok(response);
    }

    public static async Task<IResult> GetCurrentAccount(
        HttpContext context,
        GetCurrentAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var principalUser = CurrentUserProfileReader.Read(context.User);
        if (principalUser is null)
        {
            return Results.Unauthorized();
        }

        var account = await handler.HandleAsync(principalUser.UserId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(account);
    }

    public static async Task<IResult> ResolveAccounts(
        ResolveAccountsRequest request,
        IMarketDataStore store,
        CancellationToken cancellationToken)
    {
        var requestedUserIds = (request.UserIds ?? [])
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Select(userId => userId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(50)
            .ToList();

        if (requestedUserIds.Count == 0)
        {
            return Results.Ok(Array.Empty<AccountIdentityDto>());
        }

        var orderLookup = requestedUserIds
            .Select((userId, index) => new { userId, index })
            .ToDictionary(x => x.userId, x => x.index, StringComparer.OrdinalIgnoreCase);

        var accounts = await store.GetAccountsAsync(requestedUserIds, cancellationToken).ConfigureAwait(false);
        var response = accounts
            .OrderBy(account => orderLookup[account.UserId])
            .Select(account => new AccountIdentityDto(account.UserId, account.DisplayName, account.Email))
            .ToList();

        return Results.Ok(response);
    }

    private static DemoAccountDto ToDemoAccountDto(DemoAccount account)
    {
        return new DemoAccountDto(
            account.UserId,
            account.DisplayName,
            account.Email,
            account.CashAvailable,
            account.Holdings
                .OrderBy(holding => holding.Key, StringComparer.OrdinalIgnoreCase)
                .Select(holding => new DemoHoldingDto(holding.Key, holding.Value))
                .ToList());
    }
}
