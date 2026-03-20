using Trading.Contracts.Http;

namespace MarketService.ContractTests;

public sealed class MarketRealtimeContractsTests
{
    [Fact]
    public void Funds_updated_payload_uses_expected_shape()
    {
        var changedAtUtc = new DateTimeOffset(2026, 3, 19, 15, 30, 0, TimeSpan.Zero);

        var payload = new FundsUpdatedRealtimeDto("user-1", 9950m, changedAtUtc);

        Assert.Equal("user-1", payload.UserId);
        Assert.Equal(9950m, payload.CashAvailable);
        Assert.Equal(changedAtUtc, payload.ChangedAtUtc);
        Assert.Equal(1, payload.Version);
    }
}
