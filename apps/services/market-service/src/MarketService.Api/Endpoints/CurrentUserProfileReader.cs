using System.Security.Claims;
using MarketService.Application.Accounts;
using MarketService.Application.Orders;

namespace MarketService.Api.Endpoints;

internal static class CurrentUserProfileReader
{
    /// <inheritdoc cref="UserEmailClaimResolver.TryStrictEmailClaim" />
    public static string? TryGetEmailClaim(ClaimsPrincipal user) =>
        UserEmailClaimResolver.TryStrictEmailClaim(user);

    public static MarketUserProfile? Read(ClaimsPrincipal user)
    {
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var email = UserEmailClaimResolver.TryResolve(user);

        var displayName = MarketUserIdentityDefaults.NormalizeDisplayName(
            user.FindFirst("name")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("nickname")?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? email
            ?? userId,
            userId);

        return new MarketUserProfile(userId, displayName, email);
    }

    public static MarketUserProfile FromBootstrap(string userId, string? displayName, string? email)
    {
        var resolvedEmail = string.IsNullOrWhiteSpace(email)
            ? null
            : MarketUserIdentityDefaults.NormalizeEmail(email, userId);
        return new MarketUserProfile(
            userId,
            MarketUserIdentityDefaults.NormalizeDisplayName(displayName, userId),
            resolvedEmail);
    }
}
