using SettlementService.Domain.Entities;
using Trading.Contracts.Events;

namespace SettlementService.Application.Consumers;

public sealed class SettlementStateMachine
{
    public (SettlementStarted Started, SettlementCompleted Completed, SettlementStatus FinalStatus) Progress(Settlement settlement, bool simulateFailure)
    {
        settlement.Status = SettlementStatus.IN_PROGRESS;
        var startedAt = DateTimeOffset.UtcNow;
        settlement.StartedAtUtc = startedAt;

        var started = new SettlementStarted(
            Guid.NewGuid(),
            startedAt,
            settlement.CorrelationId,
            "settlement-service",
            settlement.SettlementId,
            settlement.TradeId,
            settlement.BuyerUserId,
            settlement.SellerUserId,
            SettlementStatus.IN_PROGRESS.ToString(),
            startedAt);

        settlement.CompletedAtUtc = startedAt.AddSeconds(1);
        settlement.Status = simulateFailure ? SettlementStatus.FAILED : SettlementStatus.SETTLED;
        settlement.FailureReason = simulateFailure ? "Simulated settlement failure." : null;

        var completed = new SettlementCompleted(
            Guid.NewGuid(),
            settlement.CompletedAtUtc.Value,
            settlement.CorrelationId,
            "settlement-service",
            settlement.SettlementId,
            settlement.TradeId,
            settlement.BuyerUserId,
            settlement.SellerUserId,
            settlement.Status.ToString(),
            settlement.CompletedAtUtc.Value,
            settlement.FailureReason);
        return (started, completed, settlement.Status);
    }
}
