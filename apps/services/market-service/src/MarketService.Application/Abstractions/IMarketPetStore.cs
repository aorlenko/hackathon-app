namespace MarketService.Application.Abstractions;

public interface IMarketPetStore
{
    Task<bool> IsAuthorizedTraderCommandAsync(
        Guid traderId,
        string? authenticatedUserSub,
        CancellationToken cancellationToken = default);

    /// <summary>Finds or creates a pet trader row keyed by Auth0 / OIDC <paramref name="externalUserSub"/>.</summary>
    Task<Guid> EnsureLinkedTraderForUserAsync(
        string externalUserSub,
        string displayName,
        string? email = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BreedSupplyRow>> GetBreedsWithSupplyAsync(CancellationToken cancellationToken = default);

    Task<PurchasePetsResult?> PurchasePetsAsync(
        Guid traderId,
        Guid breedId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<TraderSnapshotRow?> GetTraderSnapshotAsync(
        Guid traderId,
        CancellationToken cancellationToken = default);

    /// <param name="viewerTraderId">When set, active below-ask bids are included only for that trader's own listings (seller view).</param>
    Task<IReadOnlyList<MarketListingRow>> GetMarketListingsAsync(
        Guid? viewerTraderId = null,
        CancellationToken cancellationToken = default);

    Task<Guid?> CreateListingAsync(Guid traderId, Guid petId, decimal askingPrice, CancellationToken cancellationToken = default);

    Task<bool> WithdrawListingAsync(Guid traderId, Guid listingId, CancellationToken cancellationToken = default);

    Task<PlaceBidResult?> PlaceBidAsync(Guid traderId, Guid listingId, decimal amount, CancellationToken cancellationToken = default);

    Task<bool> WithdrawBidAsync(Guid traderId, Guid bidId, CancellationToken cancellationToken = default);

    Task<TradeResultRow?> AcceptBidAsync(Guid traderId, Guid listingId, CancellationToken cancellationToken = default);

    /// <returns>Whether the reject succeeded and the buyer trader id (for realtime notification fan-out).</returns>
    Task<(bool ok, Guid? buyerTraderId)> RejectBidAsync(
        Guid traderId,
        Guid listingId,
        CancellationToken cancellationToken = default);

    /// <summary>Resale (peer) trades where the given Auth0/OIDC <paramref name="externalUserSub"/> is buyer or seller.</summary>
    Task<IReadOnlyList<PetResaleTradeHistoryRow>> GetResaleTradesForExternalUserAsync(
        string externalUserSub,
        CancellationToken cancellationToken = default);

    Task<PetAnalysisRow?> GetPetAnalysisAsync(Guid petId, Guid? viewerTraderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaderboardRow>> GetLeaderboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationRow>> GetNotificationsAsync(Guid traderId, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> RunValuationTickAsync(CancellationToken cancellationToken = default);
}

public sealed record BreedSupplyRow(
    Guid Id,
    string Name,
    string Category,
    decimal LifespanYears,
    int BaselineDesirability,
    decimal MaintenanceCost,
    decimal RetailPrice,
    int RemainingSupply);

public sealed record PetSummaryRow(
    Guid Id,
    string BreedName,
    decimal AgeYears,
    decimal Health,
    int CurrentDesirability,
    decimal IntrinsicValue,
    bool IsExpired,
    decimal MaintenanceCost);

public sealed record BidStatusRow(
    Guid BidId,
    Guid ListingId,
    Guid PetId,
    decimal Amount,
    string Status);

public sealed record TraderSnapshotRow(
    Guid TraderId,
    string DisplayName,
    decimal AvailableCash,
    decimal LockedCash,
    decimal PortfolioTotal,
    IReadOnlyList<PetSummaryRow> Pets,
    IReadOnlyList<BidStatusRow> MyBids);

public sealed record PurchasePetsResult(IReadOnlyList<PetSummaryRow> Pets, decimal AvailableCash);

public sealed record MarketListingRow(
    Guid ListingId,
    Guid PetId,
    Guid SellerTraderId,
    string BreedName,
    decimal AskingPrice,
    string SellerDisplayName,
    string? SellerEmail,
    DateTimeOffset CreatedAt,
    decimal? RecentTradePriceForBreed,
    int RemainingNewSupplyForBreed,
    decimal? ActiveBidAmount,
    string? ActiveBidBuyerDisplayName);

public sealed record TradeResultRow(
    Guid TradeId,
    Guid PetId,
    Guid BuyerTraderId,
    Guid SellerTraderId,
    decimal Price,
    DateTimeOffset ExecutedAt);

/// <summary>DTO aligned with SPA <c>TradeRecord</c> for merging with trade-service history.</summary>
public sealed record PetResaleTradeHistoryRow(
    Guid TradeId,
    string Symbol,
    decimal Price,
    int Quantity,
    DateTimeOffset ExecutedAt,
    string BuyerUserId,
    string SellerUserId);

public abstract record PlaceBidResult;

public sealed record ActiveBidPlaceResult(Guid BidId) : PlaceBidResult;

public sealed record CrossTradePlaceResult(TradeResultRow Trade) : PlaceBidResult;

public sealed record PetAnalysisRow(
    Guid PetId,
    string BreedName,
    decimal AgeYears,
    decimal Health,
    int CurrentDesirability,
    decimal MaintenanceCost,
    decimal IntrinsicValue,
    bool IsExpired);

public sealed record LeaderboardRow(Guid TraderId, string DisplayName, decimal PortfolioTotal, int Rank);

public sealed record NotificationRow(
    Guid Id,
    string Type,
    DateTimeOffset CreatedAt,
    Guid PetId,
    string PetName,
    decimal? Amount,
    Guid CounterpartyTraderId,
    string CounterpartyDisplayName);
