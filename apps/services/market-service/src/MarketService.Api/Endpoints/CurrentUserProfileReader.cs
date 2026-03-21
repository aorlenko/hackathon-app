using System.Security.Claims;
using MarketService.Application.Accounts;
using MarketService.Application.Orders;

namespace MarketService.Api.Endpoints;

internal static class CurrentUserProfileReader
{
    /// <summary>
    /// Raw email from bearer claims, or null when the access token does not carry an email claim.
    /// Do not substitute a placeholder here — that breaks bootstrap fallbacks (request body vs JWT).
    /// </summary>
    public static string? TryGetEmailClaim(ClaimsPrincipal user)
    {
        var v = user.FindFirst("email")?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value;
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    public static MarketUserProfile? Read(ClaimsPrincipal user)
    {
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var displayName = MarketUserIdentityDefaults.NormalizeDisplayName(
            user.FindFirst("name")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("nickname")?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? TryGetEmailClaim(user)
            ?? userId,
            userId);
        var email = TryGetEmailClaim(user);

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
