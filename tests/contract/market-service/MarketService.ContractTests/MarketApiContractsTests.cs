using MarketService.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Trading.Contracts.Http;
using Trading.TestSupport;

namespace MarketService.ContractTests;

public sealed class MarketApiContractsTests
{
    [Fact]
    public async Task Post_orders_returns_accepted_response_shape()
    {
        var harness = new TradingPlatformHarness();

        var response = await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 5));

        Assert.NotEqual(Guid.Empty, response.Response.OrderId);
        Assert.Equal("OPEN", response.Response.Status);
        Assert.Equal(5, response.Response.RemainingQuantity);
    }

    [Fact]
    public async Task Get_markets_returns_seeded_catalog()
    {
        var harness = new TradingPlatformHarness();

        var markets = await harness.GetMarkets.HandleAsync();

        Assert.Contains(markets, market => market.Symbol == "ABC");
        Assert.All(markets, market => Assert.False(string.IsNullOrWhiteSpace(market.Name)));
    }

    [Fact]
    public async Task Get_order_book_returns_aggregated_levels()
    {
        var harness = new TradingPlatformHarness();
        await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 100m, 2));
        await harness.PlaceOrderAsync("user-1", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 100m, 3));

        var orderBook = await harness.GetOrderBook.HandleAsync("ABC");

        Assert.Single(orderBook.Bids);
        Assert.Equal(5, orderBook.Bids[0].Quantity);
        Assert.Equal(2, orderBook.Bids[0].OrderCount);
    }

    [Fact]
    public async Task Post_orders_auto_provisions_demo_account_for_first_time_user()
    {
        var harness = new TradingPlatformHarness();

        var response = await harness.PlaceOrderAsync("auth0|first-time-user", new PlaceOrderRequest("ABC", OrderSideDto.BUY, 101m, 5));
        var account = await harness.MarketStore.GetAccountAsync("auth0|first-time-user");

        Assert.Equal("OPEN", response.Response.Status);
        Assert.NotNull(account);
        Assert.True(account!.CashAvailable > 0);
        Assert.Empty(account.Holdings);
    }

    [Fact]
    public async Task Post_accounts_bootstrap_provisions_demo_account_on_login()
    {
        var harness = new TradingPlatformHarness();
        var httpContext = new DefaultHttpContext
        {
            User = TradingPlatformHarness.CreatePrincipal(
                "auth0|bootstrap-user",
                "Ignored Claim Name")
        };

        var result = await AccountsEndpoints.BootstrapAccount(
            httpContext,
            new BootstrapDemoAccountRequest("Bootstrap Trader", "bootstrap@example.com"),
            harness.MarketStore,
            CancellationToken.None);
        var account = await harness.MarketStore.GetAccountAsync("auth0|bootstrap-user");

        var ok = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<DemoAccountDto>>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal("auth0|bootstrap-user", ok.Value!.UserId);
        Assert.Equal("Bootstrap Trader", ok.Value.DisplayName);
        Assert.Equal("bootstrap@example.com", ok.Value.Email);
        Assert.NotNull(account);
        Assert.Empty(account!.Holdings);
        Assert.Equal("Bootstrap Trader", account.DisplayName);
        Assert.Equal("bootstrap@example.com", account.Email);
    }

    [Fact]
    public async Task Get_accounts_me_returns_current_account_snapshot_shape()
    {
        var harness = new TradingPlatformHarness();
        var httpContext = new DefaultHttpContext
        {
            User = TradingPlatformHarness.CreatePrincipal("user-1", "Buyer One", "user1@example.com")
        };

        var result = await AccountsEndpoints.GetCurrentAccount(
            httpContext,
            harness.GetCurrentAccount,
            CancellationToken.None);

        var ok = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<AccountSnapshotDto>>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal("user-1", ok.Value!.UserId);
        Assert.Equal("Buyer One", ok.Value.DisplayName);
        Assert.Equal("user1@example.com", ok.Value.Email);
        Assert.True(ok.Value.CashAvailable > 0);
        Assert.Contains(ok.Value.Holdings, holding => holding.Symbol == "ABC");
    }
}
