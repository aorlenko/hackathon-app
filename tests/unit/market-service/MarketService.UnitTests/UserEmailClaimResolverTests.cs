using System.Security.Claims;
using MarketService.Application.Accounts;

namespace MarketService.UnitTests;

public sealed class UserEmailClaimResolverTests
{
    [Fact]
    public void TryResolve_returns_email_claim_first()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("email", "  from-claim@test.dev  "),
            new Claim("preferred_username", "from-preferred@test.dev"),
        }));

        Assert.Equal("from-claim@test.dev", UserEmailClaimResolver.TryResolve(user));
    }

    [Fact]
    public void TryResolve_falls_back_to_preferred_username_when_looks_like_email()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "googleuser@gmail.com"),
        }));

        Assert.Equal("googleuser@gmail.com", UserEmailClaimResolver.TryResolve(user));
    }

    [Fact]
    public void TryResolve_ignores_preferred_username_that_is_not_an_email()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "johndoe123"),
        }));

        Assert.Null(UserEmailClaimResolver.TryResolve(user));
    }
}
