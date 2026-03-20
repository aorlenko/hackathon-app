namespace MarketService.Application.Accounts;

public static class MarketUserIdentityDefaults
{
    private const string PlaceholderEmailDomain = "demo.local";

    public static string NormalizeDisplayName(string? displayName, string userId)
    {
        if (!string.IsNullOrWhiteSpace(displayName) && !string.Equals(displayName.Trim(), userId, StringComparison.Ordinal))
        {
            return displayName.Trim();
        }

        var suffix = GetUserSuffix(userId);
        return $"Trader {suffix}";
    }

    public static string NormalizeEmail(string? email, string userId)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            return email.Trim();
        }

        var suffix = GetUserSuffix(userId).ToLowerInvariant();
        return $"trader-{suffix}@{PlaceholderEmailDomain}";
    }

    public static string GetUserSuffix(string userId)
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
