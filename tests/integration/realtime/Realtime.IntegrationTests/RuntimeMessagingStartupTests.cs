using MarketService.Api.Hubs;
using MarketService.Application.Abstractions;
using MarketService.Application.Realtime;
using MarketService.Host;
using MarketService.Infrastructure.Persistence;
using MarketService.Infrastructure.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SettlementService.Api.Endpoints;
using SettlementService.Application.Abstractions;
using SettlementService.Domain.Entities;
using SettlementService.Host;
using SettlementService.Infrastructure.Persistence;
using TradeService.Application.Abstractions;
using TradeService.Host;
using TradeService.Infrastructure.Persistence;
using Trading.Configuration;
using Trading.Contracts.Events;
using Trading.Contracts.Http;
using Trading.Messaging;

namespace Realtime.IntegrationTests;

public sealed class RuntimeMessagingStartupTests
{
    [Fact]
    public async Task Trade_service_startup_processes_order_matched_messages()
    {
        var databaseName = $"trade-runtime-{Guid.NewGuid():N}";
        using var factory = new WebApplicationFactory<TradeService.Host.EntryPointMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    ConfigureInMemoryMessaging(services, "trade-service.on-order-matched");
                    services.RemoveAll<DbContextOptions<TradeDbContext>>();
                    services.RemoveAll<ITradeDataStore>();
                    services.AddDbContext<TradeDbContext>(options => options.UseInMemoryDatabase(databaseName));
                    services.AddScoped<ITradeDataStore>(sp => sp.GetRequiredService<TradeDbContext>());
                });
            });

        using var client = factory.CreateClient();
        var message = CreateOrderMatched();

        await factory.Services.GetRequiredService<IEventBus>().PublishAsync(message);

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
        var trade = await store.GetTradeByIdAsync(message.MatchId);

        Assert.NotNull(trade);
        Assert.Equal(message.MatchId, trade!.TradeId);
        Assert.Equal(message.Symbol, trade.Symbol);
    }

    [Fact]
    public async Task Settlement_service_startup_processes_trade_recorded_messages()
    {
        var databaseName = $"settlement-runtime-{Guid.NewGuid():N}";
        using var factory = new WebApplicationFactory<SettlementService.Host.EntryPointMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    ConfigureInMemoryMessaging(services, "settlement-service.on-trade-recorded");
                    services.RemoveAll<DbContextOptions<SettlementDbContext>>();
                    services.RemoveAll<ISettlementDataStore>();
                    services.AddDbContext<SettlementDbContext>(options => options.UseInMemoryDatabase(databaseName));
                    services.AddScoped<ISettlementDataStore>(sp => sp.GetRequiredService<SettlementDbContext>());
                });
            });

        using var client = factory.CreateClient();
        var message = CreateTradeRecorded();

        await factory.Services.GetRequiredService<IEventBus>().PublishAsync(message);

        await using var scope = factory.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<SettlementDbContext>();
        var settlement = await store.GetSettlementByTradeIdAsync(message.TradeId);

        Assert.NotNull(settlement);
        Assert.Equal(message.BuyerUserId, settlement!.BuyerUserId);
        Assert.Equal(SettlementStatus.SETTLED, settlement.Status);
    }

    [Fact]
    public async Task Market_service_startup_relays_trade_and_settlement_messages()
    {
        var fakePublisher = new FakeMarketHubPublisher();
        var databaseName = $"market-runtime-{Guid.NewGuid():N}";

        using var factory = new WebApplicationFactory<MarketService.Host.EntryPointMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    ConfigureInMemoryMessaging(services, "market-service.relay-realtime");
                    services.RemoveAll<DbContextOptions<MarketDbContext>>();
                    services.RemoveAll<IMarketDataStore>();
                    services.RemoveAll<IMarketHubPublisher>();
                    services.AddDbContext<MarketDbContext>(options => options.UseInMemoryDatabase(databaseName));
                    services.AddScoped<IMarketDataStore>(sp => sp.GetRequiredService<MarketDbContext>());
                    services.AddSingleton<IMarketHubPublisher>(fakePublisher);
                });
            });

        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<MarketDbContext>();
            await SeedDataRunner.SeedAsync(store);
        }
        var trade = CreateTradeRecorded();
        var settlement = new SettlementCompleted(
            Guid.NewGuid(),
            trade.ExecutedAtUtc.AddSeconds(1),
            trade.CorrelationId,
            "settlement-service",
            Guid.NewGuid(),
            trade.TradeId,
            trade.BuyerUserId,
            trade.SellerUserId,
            SettlementStatus.SETTLED.ToString(),
            trade.ExecutedAtUtc.AddSeconds(1),
            null);

        var bus = factory.Services.GetRequiredService<IEventBus>();
        await bus.PublishAsync(trade);
        await bus.PublishAsync(settlement);

        Assert.Single(fakePublisher.Trades);
        Assert.Single(fakePublisher.Settlements);
        Assert.Equal(trade.Symbol, fakePublisher.Trades[0].Symbol);
        Assert.Equal(trade.BuyerUserId, fakePublisher.Trades[0].Payload.BuyerUserId);
        Assert.Equal(trade.SellerUserId, fakePublisher.Trades[0].Payload.SellerUserId);
        Assert.Contains(trade.BuyerUserId, fakePublisher.Settlements[0].UserIds);
        Assert.Contains(trade.SellerUserId, fakePublisher.Settlements[0].UserIds);
    }

    private static void ConfigureInMemoryMessaging(IServiceCollection services, string subscriptionName)
    {
        services.PostConfigure<TradingPlatformOptions>(options =>
        {
            options.Messaging.Transport = MessagingTransport.InMemory;
            options.Messaging.TopicName = "trading.lifecycle";
            options.Messaging.SubscriptionName = subscriptionName;
        });
    }

    private static OrderMatched CreateOrderMatched()
    {
        var now = DateTimeOffset.UtcNow;
        return new OrderMatched(
            Guid.NewGuid(),
            now,
            Guid.NewGuid().ToString("N"),
            "market-service",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user-1",
            "user-2",
            Guid.NewGuid(),
            "ABC",
            101m,
            5,
            now);
    }

    private static TradeRecorded CreateTradeRecorded()
    {
        var now = DateTimeOffset.UtcNow;
        return new TradeRecorded(
            Guid.NewGuid(),
            now,
            Guid.NewGuid().ToString("N"),
            "trade-service",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user-1",
            "user-2",
            Guid.NewGuid(),
            "ABC",
            101m,
            5,
            now);
    }

    private sealed class FakeMarketHubPublisher : IMarketHubPublisher
    {
        public List<(string Symbol, TradeRecordedRealtimeDto Payload)> Trades { get; } = [];
        public List<(IReadOnlyList<string> UserIds, SettlementUpdatedRealtimeDto Payload)> Settlements { get; } = [];

        public Task PublishOrderBookAsync(string symbol, OrderBookDto payload, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishTradeAsync(string symbol, TradeRecordedRealtimeDto payload, CancellationToken cancellationToken = default)
        {
            Trades.Add((symbol, payload));
            return Task.CompletedTask;
        }

        public Task PublishFundsUpdatedAsync(string userId, FundsUpdatedRealtimeDto payload, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task PublishSettlementAsync(IEnumerable<string> userIds, SettlementUpdatedRealtimeDto payload, CancellationToken cancellationToken = default)
        {
            Settlements.Add((userIds.ToList(), payload));
            return Task.CompletedTask;
        }
    }
}
