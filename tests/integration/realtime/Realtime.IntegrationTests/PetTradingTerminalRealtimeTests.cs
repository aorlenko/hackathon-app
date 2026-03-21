using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using MarketService.Application.Abstractions;
using MarketService.Application.Realtime;
using MarketService.Host;
using MarketService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Realtime.IntegrationTests;

public sealed class PetTradingTerminalRealtimeTests
{
    private const string TestAuthScheme = "Test";
    private static readonly Guid PoodleBreedId = Guid.Parse("44444444-4444-4444-4444-000000000003");

    [Fact]
    public async Task Buy_now_refreshes_buyer_and_seller_terminal_groups()
    {
        var publisher = new FakeTradingPetsRealtimePublisher();
        using var factory = CreateFactory(publisher);
        using var client = factory.CreateClient();

        Guid sellerTraderId;
        Guid buyerTraderId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IMarketPetStore>();
            sellerTraderId = await store.EnsureLinkedTraderForUserAsync("seller-user", "Seller User", "seller@example.com");
            buyerTraderId = await store.EnsureLinkedTraderForUserAsync("buyer-user", "Buyer User", "buyer@example.com");
            await store.PurchasePetsAsync(sellerTraderId, PoodleBreedId, 1);
            await store.PlaceTerminalAskAsync(sellerTraderId, PoodleBreedId, 1, 88m);
        }

        using var request = CreateAuthedRequest(
            HttpMethod.Post,
            "/api/pets/terminal/orders/buy-now",
            "buyer-user",
            "Buyer User",
            "buyer@example.com");
        request.Content = JsonContent.Create(new
        {
            marketEntryId = PoodleBreedId,
            quantity = 1
        });

        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Contains(buyerTraderId, publisher.SnapshotRefreshes);
        Assert.Contains(sellerTraderId, publisher.SnapshotRefreshes);
        Assert.Contains(buyerTraderId, publisher.NotificationRefreshes);
        Assert.Contains(sellerTraderId, publisher.NotificationRefreshes);
        Assert.True(publisher.MarketRefreshCount >= 1);
        Assert.True(publisher.LeaderboardRefreshCount >= 1);
    }

    [Fact]
    public async Task Higher_terminal_bid_refreshes_seller_and_outbid_trader_groups()
    {
        var publisher = new FakeTradingPetsRealtimePublisher();
        using var factory = CreateFactory(publisher);
        using var client = factory.CreateClient();

        Guid sellerTraderId;
        Guid previousBidderTraderId;
        Guid newBidderTraderId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IMarketPetStore>();
            sellerTraderId = await store.EnsureLinkedTraderForUserAsync("seller-user", "Seller User", "seller@example.com");
            previousBidderTraderId = await store.EnsureLinkedTraderForUserAsync("prior-bidder", "Prior Bidder", "prior@example.com");
            newBidderTraderId = await store.EnsureLinkedTraderForUserAsync("new-bidder", "New Bidder", "new@example.com");

            await store.PurchasePetsAsync(sellerTraderId, PoodleBreedId, 1);
            await store.PlaceTerminalAskAsync(sellerTraderId, PoodleBreedId, 1, 100m);
            await store.PlaceTerminalBidAsync(previousBidderTraderId, PoodleBreedId, 1, 90m);
        }

        using var request = CreateAuthedRequest(
            HttpMethod.Post,
            "/api/pets/terminal/orders/bid",
            "new-bidder",
            "New Bidder",
            "new@example.com");
        request.Content = JsonContent.Create(new
        {
            marketEntryId = PoodleBreedId,
            quantity = 1,
            limitPrice = 95m
        });

        using var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Contains(newBidderTraderId, publisher.SnapshotRefreshes);
        Assert.Contains(sellerTraderId, publisher.SnapshotRefreshes);
        Assert.Contains(previousBidderTraderId, publisher.SnapshotRefreshes);
        Assert.Contains(newBidderTraderId, publisher.NotificationRefreshes);
        Assert.Contains(sellerTraderId, publisher.NotificationRefreshes);
        Assert.Contains(previousBidderTraderId, publisher.NotificationRefreshes);
        Assert.True(publisher.MarketRefreshCount >= 1);
        Assert.True(publisher.LeaderboardRefreshCount >= 1);
    }

    private static WebApplicationFactory<EntryPointMarker> CreateFactory(
        FakeTradingPetsRealtimePublisher publisher)
    {
        var databaseName = $"market-terminal-realtime-{Guid.NewGuid():N}";
        return new WebApplicationFactory<EntryPointMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<MarketDbContext>>();
                    services.RemoveAll<ITradingPetsRealtimePublisher>();
                    services.RemoveAll<IHostedService>();

                    services.AddDbContext<MarketDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                    services.AddSingleton<ITradingPetsRealtimePublisher>(publisher);
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthScheme;
                        options.DefaultChallengeScheme = TestAuthScheme;
                    }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthScheme,
                        _ => { });
                });
            });
    }

    private static HttpRequestMessage CreateAuthedRequest(
        HttpMethod method,
        string url,
        string userSub,
        string displayName,
        string email)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("x-test-sub", userSub);
        request.Headers.Add("x-test-name", displayName);
        request.Headers.Add("x-test-email", email);
        return request;
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var sub = Request.Headers["x-test-sub"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(sub))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing test subject header."));
            }

            var displayName = Request.Headers["x-test-name"].FirstOrDefault() ?? sub;
            var email = Request.Headers["x-test-email"].FirstOrDefault() ?? $"{sub}@example.com";
            var identity = new ClaimsIdentity(
                [
                    new Claim("sub", sub),
                    new Claim(ClaimTypes.NameIdentifier, sub),
                    new Claim("name", displayName),
                    new Claim(ClaimTypes.Name, displayName),
                    new Claim("email", email)
                ],
                TestAuthScheme);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(
                AuthenticateResult.Success(
                    new AuthenticationTicket(principal, TestAuthScheme)));
        }
    }

    private sealed class FakeTradingPetsRealtimePublisher : ITradingPetsRealtimePublisher
    {
        public List<Guid> SnapshotRefreshes { get; } = [];
        public List<Guid> NotificationRefreshes { get; } = [];
        public int MarketRefreshCount { get; private set; }
        public int LeaderboardRefreshCount { get; private set; }

        public Task NotifyTraderSnapshotRefreshAsync(Guid traderId, CancellationToken cancellationToken = default)
        {
            SnapshotRefreshes.Add(traderId);
            return Task.CompletedTask;
        }

        public Task NotifyMarketListingsRefreshAsync(CancellationToken cancellationToken = default)
        {
            MarketRefreshCount++;
            return Task.CompletedTask;
        }

        public Task NotifyTraderNotificationsAsync(Guid traderId, CancellationToken cancellationToken = default)
        {
            NotificationRefreshes.Add(traderId);
            return Task.CompletedTask;
        }

        public Task NotifyLeaderboardRefreshAsync(CancellationToken cancellationToken = default)
        {
            LeaderboardRefreshCount++;
            return Task.CompletedTask;
        }

        public Task NotifyPetValuationBatchAsync(
            IReadOnlyList<Guid> petIds,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
