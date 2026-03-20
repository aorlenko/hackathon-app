using System.Security.Claims;
using MarketService.Application.Accounts;
using MarketService.Application.Orders;

namespace MarketService.Api.Endpoints;

internal static class CurrentUserProfileReader
{
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
            ?? user.FindFirst("email")?.Value
            ?? userId,
            userId);
        var email = MarketUserIdentityDefaults.NormalizeEmail(user.FindFirst("email")?.Value, userId);

        return new MarketUserProfile(userId, displayName, email);
    }

    public static MarketUserProfile FromBootstrap(string userId, string? displayName, string? email)
    {
        return new MarketUserProfile(
            userId,
            MarketUserIdentityDefaults.NormalizeDisplayName(displayName, userId),
            MarketUserIdentityDefaults.NormalizeEmail(email, userId));
    }
}
