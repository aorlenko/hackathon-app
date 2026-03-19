using System.Security.Claims;
using MarketService.Application.Orders;

namespace MarketService.Api.Endpoints;

internal static class CurrentUserProfileReader
{
    private const string PlaceholderEmailDomain = "demo.local";

    public static MarketUserProfile? Read(ClaimsPrincipal user)
    {
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var displayName = NormalizeDisplayName(
            user.FindFirst("name")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("nickname")?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst("email")?.Value
            ?? userId,
            userId);
        var email = NormalizeEmail(user.FindFirst("email")?.Value, userId);

        return new MarketUserProfile(userId, displayName, email);
    }

    public static MarketUserProfile FromBootstrap(string userId, string? displayName, string? email)
    {
        return new MarketUserProfile(
            userId,
            NormalizeDisplayName(displayName, userId),
            NormalizeEmail(email, userId));
    }

    private static string NormalizeDisplayName(string? displayName, string userId)
    {
        if (!string.IsNullOrWhiteSpace(displayName) && !string.Equals(displayName.Trim(), userId, StringComparison.Ordinal))
        {
            return displayName.Trim();
        }

        var suffix = GetUserSuffix(userId);
        return $"Trader {suffix}";
    }

    private static string NormalizeEmail(string? email, string userId)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            return email.Trim();
        }

        var suffix = GetUserSuffix(userId).ToLowerInvariant();
        return $"trader-{suffix}@{PlaceholderEmailDomain}";
    }

    private static string GetUserSuffix(string userId)
    {
        var identifier = userId.Contains('|', StringComparison.Ordinal)
            ? userId[(userId.LastIndexOf('|') + 1)..]
            : userId;

        var normalized = new string(identifier.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "User";
        }

        return normalized.Length <= 8 ? normalized : normalized[..8];
    }
}
