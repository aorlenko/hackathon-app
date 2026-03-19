using System.Security.Claims;
using MarketService.Api.Hubs;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace Realtime.IntegrationTests;

public sealed class MarketHubRealtimeTests
{
    [Fact]
    public async Task Join_market_and_connect_add_expected_groups()
    {
        var groups = new RecordingGroupManager();
        var hub = new MarketHub
        {
            Context = new TestHubCallerContext("connection-1", new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "Test"))),
            Groups = groups
        };

        await hub.OnConnectedAsync();
        await hub.JoinMarket("abc");
        await hub.LeaveMarket("abc");

        Assert.Contains(("connection-1", "user:user-1:settlements"), groups.Added);
        Assert.Contains(("connection-1", "market:ABC:orderbook"), groups.Added);
        Assert.Contains(("connection-1", "market:ABC:orderbook"), groups.Removed);
    }

    private sealed class RecordingGroupManager : IGroupManager
    {
        public List<(string ConnectionId, string GroupName)> Added { get; } = [];
        public List<(string ConnectionId, string GroupName)> Removed { get; } = [];

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Added.Add((connectionId, groupName));
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Removed.Add((connectionId, groupName));
            return Task.CompletedTask;
        }
    }

    private sealed class TestHubCallerContext : HubCallerContext
    {
        private readonly string _connectionId;
        private readonly ClaimsPrincipal _user;

        public TestHubCallerContext(string connectionId, ClaimsPrincipal user)
        {
            _connectionId = connectionId;
            _user = user;
        }

        public override string ConnectionId => _connectionId;
        public override string? UserIdentifier => _user.FindFirst("sub")?.Value;
        public override ClaimsPrincipal? User => _user;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }
}
