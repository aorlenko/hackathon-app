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

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SettlementGroup(userId)).ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public static string OrderBookGroup(string symbol) => $"market:{symbol.ToUpperInvariant()}:orderbook";
    public static string TradesGroup(string symbol) => $"market:{symbol.ToUpperInvariant()}:trades";
    public static string SettlementGroup(string userId) => $"user:{userId}:settlements";
}
