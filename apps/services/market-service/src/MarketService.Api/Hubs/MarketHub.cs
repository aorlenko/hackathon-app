using Microsoft.AspNetCore.SignalR;

namespace MarketService.Api.Hubs;

public sealed class MarketHub : Hub
{
    public async Task JoinMarket(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, OrderBookGroup(symbol)).ConfigureAwait(false);
        await Groups.AddToGroupAsync(Context.ConnectionId, TradesGroup(symbol)).ConfigureAwait(false);
    }

    public async Task LeaveMarket(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, OrderBookGroup(symbol)).ConfigureAwait(false);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, TradesGroup(symbol)).ConfigureAwait(false);
    }

    /// <summary>Accept string from JSON clients to avoid Guid serialization edge cases.</summary>
    public Task SubscribeTradingPetsTrader(string traderId)
    {
        if (!Guid.TryParse(traderId, out var id))
        {
            throw new HubException("Invalid traderId.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, TradingPetsTraderGroup(id));
    }

    public Task UnsubscribeTradingPetsTrader(string traderId)
    {
        if (!Guid.TryParse(traderId, out var id))
        {
            return Task.CompletedTask;
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, TradingPetsTraderGroup(id));
    }

    public Task SubscribeTradingPetsMarket() =>
        Groups.AddToGroupAsync(Context.ConnectionId, TradingPetsMarketGroup);

    public Task SubscribeTradingPetsLeaderboard() =>
        Groups.AddToGroupAsync(Context.ConnectionId, TradingPetsLeaderboardGroup);

    public static string TradingPetsTraderGroup(Guid traderId) => $"tp:trader:{traderId:D}";

    public const string TradingPetsMarketGroup = "tp:market";

    public const string TradingPetsLeaderboardGroup = "tp:leaderboard";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, FundsGroup(userId)).ConfigureAwait(false);
            await Groups.AddToGroupAsync(Context.ConnectionId, SettlementGroup(userId)).ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public static string OrderBookGroup(string symbol) => $"market:{symbol.ToUpperInvariant()}:orderbook";
    public static string TradesGroup(string symbol) => $"market:{symbol.ToUpperInvariant()}:trades";
    public static string FundsGroup(string userId) => $"user:{userId}:funds";
    public static string SettlementGroup(string userId) => $"user:{userId}:settlements";
}
