using TradeService.Api.Endpoints;
using TradeService.Application.Abstractions;
using TradeService.Application.Consumers;
using TradeService.Application.Queries;
using TradeService.Infrastructure.Messaging;
using TradeService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddTradingPlatformConfiguration(builder.Configuration, "trade-service");
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
builder.Services.AddTradingTelemetry("trade-service");
builder.Services.AddTradingMessaging();
builder.Services.AddDbContext<TradeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));
builder.Services.AddScoped<ITradeDataStore>(sp => sp.GetRequiredService<TradeDbContext>());
builder.Services.AddScoped<ITradeEventPublisher, TradeRecordedPublisher>();
builder.Services.AddIntegrationEventHandler<OrderMatched, OrderMatchedConsumer>();
builder.Services.AddScoped<GetRecentTradesQuery>();
builder.Services.AddScoped<GetUserTradesQuery>();

var app = builder.Build();
app.UseCors(LocalDevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapTradesEndpoints();
app.Run();

public partial class Program;
