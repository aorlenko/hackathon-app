using System.Security.Claims;
using MarketService.Application.Accounts;
using MarketService.Application.Abstractions;
using MarketService.Application.Pets;
using MarketService.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MarketService.Application.Consumers;
using MarketService.Application.Matching;
using MarketService.Application.Markets;
using MarketService.Application.Orders;
using MarketService.Application.Realtime;
using MarketService.Infrastructure.Messaging;
using MarketService.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SettlementService.Application.Abstractions;
using SettlementService.Application.Consumers;
using SettlementService.Application.Queries;
using SettlementService.Infrastructure.Messaging;
using SettlementService.Infrastructure.Persistence;
using TradeService.Application.Abstractions;
using TradeService.Application.Consumers;
using TradeService.Application.Queries;
using TradeService.Infrastructure.Messaging;
using TradeService.Infrastructure.Persistence;
using Trading.Contracts.Events;
using Trading.Contracts.Http;
using Trading.Messaging;
using Trading.TestSupport.Realtime;

namespace Trading.TestSupport;

public sealed class TradingPlatformHarness
{
    public InMemoryEventBus EventBus { get; } = new();
    public MarketDbContext MarketStore { get; }
    public TradeDbContext TradeStore { get; }
    public SettlementDbContext SettlementStore { get; }
    public CollectingMarketHubPublisher RealtimePublisher { get; } = new();

    public GetMarketsHandler GetMarkets { get; }
    public GetOrderBookHandler GetOrderBook { get; }
    public GetCurrentAccountHandler GetCurrentAccount { get; }
    public PlaceOrderHandler PlaceOrder { get; }
    public GetRecentTradesQuery GetRecentTrades { get; }
    public GetUserTradesQuery GetUserTrades { get; }
    public GetSettlementByTradeQuery GetSettlementByTrade { get; }
    public GetUserSettlementsQuery GetUserSettlements { get; }

    public TradingPlatformHarness()
    {
        MarketStore = new MarketDbContext(
            new DbContextOptionsBuilder<MarketDbContext>()
                .UseInMemoryDatabase($"market-{Guid.NewGuid():N}")
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options,
            Options.Create(TradingPetsOptions.CreateForContractTestHarness()));
        TradeStore = new TradeDbContext(new DbContextOptionsBuilder<TradeDbContext>()
            .UseInMemoryDatabase($"trade-{Guid.NewGuid():N}")
            .Options);
        SettlementStore = new SettlementDbContext(new DbContextOptionsBuilder<SettlementDbContext>()
            .UseInMemoryDatabase($"settlement-{Guid.NewGuid():N}")
            .Options);

        SeedDataRunner.SeedAsync(MarketStore).GetAwaiter().GetResult();
        SeedDataRunner.EnsureLegacyEquityCatalogAsync(MarketStore).GetAwaiter().GetResult();
        SeedDataRunner.EnsureTradingPetsSeedAsync(MarketStore, TradingPetsOptions.CreateForContractTestHarness())
            .GetAwaiter().GetResult();

        var marketPublisher = new LifecycleEventPublisher(EventBus);
        var tradePublisher = new TradeRecordedPublisher(EventBus);
        var settlementPublisher = new SettlementEventPublisher(EventBus);

        GetMarkets = new GetMarketsHandler(MarketStore);
        GetOrderBook = new GetOrderBookHandler(MarketStore);
        GetCurrentAccount = new GetCurrentAccountHandler(MarketStore);
        var applyTradeToAccounts = new ApplyTradeToAccountsHandler(MarketStore);
        var realtimeNotifier = new MarketRealtimeNotifier(GetOrderBook, RealtimePublisher);
        PlaceOrder = new PlaceOrderHandler(MarketStore, new OrderValidationPolicy(), new PriceTimeMatchingEngine(), marketPublisher, applyTradeToAccounts, realtimeNotifier);

        var orderMatchedConsumer = new OrderMatchedConsumer(TradeStore, tradePublisher);
        var tradeRelayConsumer = new TradeRecordedRelayConsumer(realtimeNotifier);
        var settlementRelayConsumer = new SettlementRelayConsumer(realtimeNotifier);
        var settlementConsumer = new TradeRecordedConsumer(SettlementStore, new SettlementStateMachine(), settlementPublisher);

        GetRecentTrades = new GetRecentTradesQuery(TradeStore);
        GetUserTrades = new GetUserTradesQuery(TradeStore);
        GetSettlementByTrade = new GetSettlementByTradeQuery(SettlementStore);
        GetUserSettlements = new GetUserSettlementsQuery(SettlementStore);

        EventBus.Subscribe<OrderMatched>((message, cancellationToken) => orderMatchedConsumer.ConsumeAsync(message, cancellationToken));
        EventBus.Subscribe<TradeRecorded>(async (message, cancellationToken) =>
        {
            await tradeRelayConsumer.HandleAsync(message, cancellationToken).ConfigureAwait(false);
            await settlementConsumer.ConsumeAsync(message, false, cancellationToken).ConfigureAwait(false);
        });
        EventBus.Subscribe<SettlementStarted>(((IIntegrationEventHandler<SettlementStarted>)settlementRelayConsumer).HandleAsync);
        EventBus.Subscribe<SettlementCompleted>(((IIntegrationEventHandler<SettlementCompleted>)settlementRelayConsumer).HandleAsync);
    }

    public Task<PlaceOrderOutcome> PlaceOrderAsync(string userId, PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        return PlaceOrder.HandleAsync(userId, request, cancellationToken);
    }

    public Task<AccountSnapshotDto?> GetCurrentAccountSnapshotAsync(string userId, CancellationToken cancellationToken = default)
    {
        return GetCurrentAccount.HandleAsync(userId, cancellationToken);
    }

    public MarketPetDataStore CreateMarketPetStore(TradingPetsOptions? options = null) =>
        new(
            MarketStore,
            NullLogger<MarketPetDataStore>.Instance,
            Options.Create(options ?? TradingPetsOptions.CreateForContractTestHarness()),
            null);

    public static ClaimsPrincipal CreatePrincipal(string userId, string? displayName = null, string? email = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId)
        };

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            claims.Add(new Claim("name", displayName));
            claims.Add(new Claim(ClaimTypes.Name, displayName));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim("email", email));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            "Test"));
    }
}
