using SettlementService.Application.Abstractions;
using Trading.Contracts.Http;

namespace SettlementService.Application.Queries;

public sealed class GetUserSettlementsQuery
{
    private readonly ISettlementDataStore _store;

    public GetUserSettlementsQuery(ISettlementDataStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<SettlementDto>> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var settlements = await _store.GetUserSettlementsAsync(userId, cancellationToken).ConfigureAwait(false);
        return settlements
            .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc)
            .Select(x => new SettlementDto(x.SettlementId, x.TradeId, x.Status.ToString(), x.StartedAtUtc, x.CompletedAtUtc, x.FailureReason, x.BuyerUserId, x.SellerUserId))
            .ToList();
    }
}
