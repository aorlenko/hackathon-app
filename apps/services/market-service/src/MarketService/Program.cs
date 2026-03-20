using MarketService.Api.Endpoints;
using MarketService.Api.Hubs;
using MarketService.Api.Realtime;
using MarketService.Application.Accounts;
using MarketService.Application.Abstractions;
using MarketService.Application.Authorization;
using MarketService.Application.Consumers;
using MarketService.Application.Matching;
using MarketService.Application.Market;
using MarketService.Application.Markets;
using MarketService.Application.Orders;
using MarketService.Application.Pets;
using MarketService.Application.Realtime;
using MarketService.Infrastructure.Messaging;
using MarketService.Infrastructure.Persistence;
using MarketService.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Trading.Auth;
using Trading.Configuration;
using Trading.Contracts.Events;
using Trading.Messaging;
using Trading.Observability;

const string LocalDevCorsPolicy = "LocalDevFrontend";
var localDevOrigins = new[]
{
    "http://localhost:5173",
    "http://localhost:5174",
    "http://127.0.0.1:5173",
    "http://127.0.0.1:5174"
};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTradingPlatformConfiguration(builder.Configuration, "market-service");
builder.Services.AddCors(options =>
{
    options.AddPolicy(LocalDevCorsPolicy, policy =>
    {
        policy.WithOrigins(localDevOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddTradingJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddTradingTelemetry("market-service");
builder.Services.AddTradingMessaging();
builder.Services.AddDbContext<MarketDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));
builder.Services.AddScoped<IMarketDataStore>(sp => sp.GetRequiredService<MarketDbContext>());
builder.Services.Configure<TradingPetsOptions>(builder.Configuration.GetSection(TradingPetsOptions.SectionName));
builder.Services.AddScoped<IMarketPetStore, MarketPetDataStore>();
builder.Services.AddScoped<TraderAuthorizationHelper>();
builder.Services.AddScoped<PurchasePetsHandler>();
builder.Services.AddScoped<SecondaryMarketHandlers>();
builder.Services.AddSingleton<ITradingPetsRealtimePublisher, SignalRTradingPetsRealtimePublisher>();
builder.Services.AddHostedService<PetValuationTickHostedService>();
builder.Services.AddScoped<OrderValidationPolicy>();
builder.Services.AddScoped<PriceTimeMatchingEngine>();
builder.Services.AddScoped<GetCurrentAccountHandler>();
builder.Services.AddScoped<ApplyTradeToAccountsHandler>();
builder.Services.AddScoped<GetMarketsHandler>();
builder.Services.AddScoped<GetOrderBookHandler>();
builder.Services.AddScoped<ILifecycleEventPublisher, LifecycleEventPublisher>();
builder.Services.AddScoped<PlaceOrderHandler>();
builder.Services.AddSingleton<IMarketHubPublisher, SignalRMarketHubPublisher>();
builder.Services.AddScoped<IMarketRealtimeNotifier, MarketRealtimeNotifier>();
builder.Services.AddIntegrationEventHandler<TradeRecorded, TradeRecordedRelayConsumer>();
builder.Services.AddIntegrationEventHandler<SettlementStarted, SettlementRelayConsumer>();
builder.Services.AddIntegrationEventHandler<SettlementCompleted, SettlementRelayConsumer>();

var app = builder.Build();
app.UseCors(LocalDevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MarketDbContext>();
    var tradingPets = scope.ServiceProvider.GetRequiredService<IOptions<TradingPetsOptions>>().Value;
    await SeedDataRunner.SeedAsync(db).ConfigureAwait(false);
    await SeedDataRunner.EnsureTradingPetsSeedAsync(db, tradingPets).ConfigureAwait(false);
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapMarketsEndpoints();
app.MapAccountsEndpoints();
app.MapOrdersEndpoints();
app.MapPetsBreedsEndpoints();
app.MapPetsPurchaseEndpoints();
app.MapPetsAnalysisEndpoints();
app.MapTradersSnapshotEndpoints();
app.MapTradersNotificationsEndpoints();
app.MapTradersLeaderboardEndpoints();
app.MapMarketTradingEndpoints();
app.MapHub<MarketHub>("/hubs/market");
app.Run();

public partial class Program;

internal sealed class SignalRMarketHubPublisher : IMarketHubPublisher
{
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<MarketHub> _hubContext;

    public SignalRMarketHubPublisher(Microsoft.AspNetCore.SignalR.IHubContext<MarketHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishOrderBookAsync(string symbol, Trading.Contracts.Http.OrderBookDto payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.Group(MarketHub.OrderBookGroup(symbol)).SendCoreAsync("OrderBookUpdated", [payload], cancellationToken);
    }

    public Task PublishTradeAsync(string symbol, Trading.Contracts.Http.TradeRecordedRealtimeDto payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.Group(MarketHub.TradesGroup(symbol)).SendCoreAsync("TradeRecorded", [payload], cancellationToken);
    }

    public Task PublishSettlementAsync(IEnumerable<string> userIds, Trading.Contracts.Http.SettlementUpdatedRealtimeDto payload, CancellationToken cancellationToken = default)
    {
        var tasks = userIds.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(userId => _hubContext.Clients.Group(MarketHub.SettlementGroup(userId)).SendCoreAsync("SettlementUpdated", [payload], cancellationToken));
        return Task.WhenAll(tasks);
    }

    public Task PublishFundsUpdatedAsync(string userId, Trading.Contracts.Http.FundsUpdatedRealtimeDto payload, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.Group(MarketHub.FundsGroup(userId)).SendCoreAsync("FundsUpdated", [payload], cancellationToken);
    }
}
