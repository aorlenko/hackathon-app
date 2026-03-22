using SettlementService.Api.Endpoints;
using SettlementService.Application.Abstractions;
using SettlementService.Application.Consumers;
using SettlementService.Application.Queries;
using SettlementService.Infrastructure.Messaging;
using SettlementService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Trading.Auth;
using Trading.Configuration;
using Trading.Contracts.Events;
using Trading.Messaging;
using Trading.Observability;

const string DemoCorsPolicy = "DemoFrontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTradingPlatformConfiguration(builder.Configuration, "settlement-service");
builder.Services.AddCors(options =>
{
    options.AddPolicy(DemoCorsPolicy, policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddTradingJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddTradingTelemetry("settlement-service");
builder.Services.AddTradingMessaging();
builder.Services.AddDbContext<SettlementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Sql")));
builder.Services.AddScoped<ISettlementDataStore>(sp => sp.GetRequiredService<SettlementDbContext>());
builder.Services.AddScoped<ISettlementEventPublisher, SettlementEventPublisher>();
builder.Services.AddScoped<SettlementStateMachine>();
builder.Services.AddIntegrationEventHandler<TradeRecorded, TradeRecordedConsumer>();
builder.Services.AddScoped<GetSettlementByTradeQuery>();
builder.Services.AddScoped<GetUserSettlementsQuery>();

var app = builder.Build();
app.UseCors(DemoCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapSettlementsEndpoints();
app.Run();

public partial class Program;
