using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Trading.Auth;

public sealed class TradingJwtOptions
{
    public string Domain { get; set; } = string.Empty;
    public string Authority { get; set; } = "https://example.auth0.com/";
    public string Audience { get; set; } = "trading-api";
}

public static class JwtAuthenticationExtensions
{
    public static AuthenticationBuilder AddTradingJwtAuthentication(this IServiceCollection services, IConfiguration configuration, string signalRHubPath = "/hubs/market")
    {
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        services.AddOptions<TradingJwtOptions>()
            .Bind(configuration.GetSection("Auth0"))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.Authority) || string.Equals(options.Authority, "https://example.auth0.com/", StringComparison.OrdinalIgnoreCase))
                {
                    options.Authority = options.Domain;
                }

                options.Authority = NormalizeAuthority(options.Authority);
                options.Audience = options.Audience?.Trim() ?? string.Empty;
            });

        var authOptions = configuration.GetSection("Auth0").Get<TradingJwtOptions>() ?? new TradingJwtOptions();
        if (string.IsNullOrWhiteSpace(authOptions.Authority) || string.Equals(authOptions.Authority, "https://example.auth0.com/", StringComparison.OrdinalIgnoreCase))
        {
            authOptions.Authority = authOptions.Domain;
        }

        authOptions.Authority = NormalizeAuthority(authOptions.Authority);
        authOptions.Audience = authOptions.Audience?.Trim() ?? string.Empty;

        if (HasRealAuthConfiguration(authOptions))
        {
            return services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authOptions.Authority;
                    options.Audience = authOptions.Audience;
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "sub"
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (!string.IsNullOrWhiteSpace(context.Token))
                            {
                                return Task.CompletedTask;
                            }

                            var accessToken = context.Request.Query["access_token"].ToString();
                            var requestPath = context.HttpContext.Request.Path;
                            if (!string.IsNullOrWhiteSpace(accessToken) &&
                                requestPath.StartsWithSegments(signalRHubPath, StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
        }

        return services.AddAuthentication("Bearer")
            .AddScheme<AuthenticationSchemeOptions, DemoBearerAuthenticationHandler>("Bearer", _ => { });
    }

    internal static bool HasRealAuthConfiguration(TradingJwtOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Authority) &&
               !string.Equals(options.Authority, "https://example.auth0.com/", StringComparison.OrdinalIgnoreCase) &&
               !string.IsNullOrWhiteSpace(options.Audience) &&
               !string.Equals(options.Audience, "trading-api", StringComparison.OrdinalIgnoreCase);
    }

    internal static string NormalizeAuthority(string? authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
        {
            return string.Empty;
        }

        var normalized = authority.Trim();
        if (!normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = $"https://{normalized}";
        }

        if (!normalized.EndsWith("/", StringComparison.Ordinal))
        {
            normalized = $"{normalized}/";
        }

        return normalized;
    }
}

internal sealed class DemoBearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DemoBearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var subject = header["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing bearer subject."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim("sub", subject)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
