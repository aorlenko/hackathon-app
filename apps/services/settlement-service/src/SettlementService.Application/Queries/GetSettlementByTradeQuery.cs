using SettlementService.Application.Abstractions;
using Trading.Contracts.Http;

namespace SettlementService.Application.Queries;

public sealed class GetSettlementByTradeQuery
{
    private readonly ISettlementDataStore _store;

    public GetSettlementByTradeQuery(ISettlementDataStore store)
    {
        _store = store;
    }

    public async Task<SettlementDto?> ExecuteAsync(Guid tradeId, CancellationToken cancellationToken = default)
    {
        var settlement = await _store.GetSettlementByTradeIdAsync(tradeId, cancellationToken).ConfigureAwait(false);
        return settlement is null ? null : Map(settlement);
    }

    private static SettlementDto Map(Domain.Entities.Settlement settlement) => new(settlement.SettlementId, settlement.TradeId, settlement.Status.ToString(), settlement.StartedAtUtc, settlement.CompletedAtUtc, settlement.FailureReason, settlement.BuyerUserId, settlement.SellerUserId);
}
