using System.Security.Claims;

namespace MarketService.Application.Accounts;

/// <summary>
/// Resolves a login email from bearer claims. API access tokens often omit <c>email</c> while still
/// carrying <c>preferred_username</c> (or UPN) shaped like an address — matching that prevents
/// provisioning <c>trader-*@demo.local</c> placeholders before bootstrap runs.
/// </summary>
public static class UserEmailClaimResolver
{
    public static string? TryResolve(ClaimsPrincipal user)
    {
        var direct = TryStrictEmailClaim(user);
        if (direct is not null)
        {
            return direct;
        }

        var preferred = user.FindFirst("preferred_username")?.Value?.Trim();
        if (LooksLikeEmailAddress(preferred))
        {
            return preferred;
        }

        var upn = user.FindFirst(ClaimTypes.Upn)?.Value?.Trim();
        if (LooksLikeEmailAddress(upn))
        {
            return upn;
        }

        return null;
    }

    /// <summary>Standard <c>email</c> / email-type claims only.</summary>
    public static string? TryStrictEmailClaim(ClaimsPrincipal user)
    {
        var v = user.FindFirst("email")?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value;
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    public static bool LooksLikeEmailAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return trimmed.Contains('@', StringComparison.Ordinal)
            && !trimmed.Contains(' ', StringComparison.Ordinal);
    }
}
