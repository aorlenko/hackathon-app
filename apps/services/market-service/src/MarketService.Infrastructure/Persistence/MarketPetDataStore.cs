using MarketService.Application.Abstractions;
using MarketService.Application.Pets;
using MarketService.Application.Realtime;
using MarketService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trading.Contracts.Http;

namespace MarketService.Infrastructure.Persistence;

public sealed class MarketPetDataStore : IMarketPetStore
{
    private readonly MarketDbContext _db;
    private readonly ILogger<MarketPetDataStore> _logger;
    private readonly TradingPetsOptions _options;
    private readonly IMarketRealtimeNotifier? _fundsRealtime;

    public MarketPetDataStore(
        MarketDbContext db,
        ILogger<MarketPetDataStore> logger,
        IOptions<TradingPetsOptions> options,
        IMarketRealtimeNotifier? fundsRealtime = null)
    {
        _db = db;
        _logger = logger;
        _options = options.Value;
        _fundsRealtime = fundsRealtime;
    }

    public async Task<bool> IsAuthorizedTraderCommandAsync(
        Guid traderId,
        string? authenticatedUserSub,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authenticatedUserSub))
        {
            return false;
        }

        var trader = await _db.Traders
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == traderId, cancellationToken)
            .ConfigureAwait(false);
        if (trader is null)
        {
            return false;
        }

        if (_options.AllowAnyAuthenticatedUserForAllTraders)
        {
            return true;
        }

        return !string.IsNullOrEmpty(trader.ExternalUserId)
            && string.Equals(trader.ExternalUserId, authenticatedUserSub, StringComparison.Ordinal);
    }

    public async Task<Guid> EnsureLinkedTraderForUserAsync(
        string externalUserSub,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUserSub);

        var normalizedName = string.IsNullOrWhiteSpace(displayName) ? "Trader" : displayName.Trim();

        var existing = await _db.Traders
            .FirstOrDefaultAsync(t => t.ExternalUserId == externalUserSub, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            await ReconcileLinkedTraderAvailableFromDemoAccountAsync(existing, cancellationToken).ConfigureAwait(false);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return existing.Id;
        }

        await _db.EnsureDemoAccountAsync(externalUserSub, normalizedName, null, cancellationToken).ConfigureAwait(false);
        var account = await _db.Accounts
            .FirstAsync(a => a.UserId == externalUserSub, cancellationToken)
            .ConfigureAwait(false);

        var trader = new Trader
        {
            Id = Guid.NewGuid(),
            DisplayName = normalizedName,
            ExternalUserId = externalUserSub,
            AvailableCash = account.CashAvailable,
            LockedCash = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Traders.Add(trader);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return trader.Id;
        }
        catch (DbUpdateException)
        {
            _db.Entry(trader).State = EntityState.Detached;
            var retry = await _db.Traders
                .FirstOrDefaultAsync(t => t.ExternalUserId == externalUserSub, cancellationToken)
                .ConfigureAwait(false);
            if (retry is not null)
            {
                await ReconcileLinkedTraderAvailableFromDemoAccountAsync(retry, cancellationToken).ConfigureAwait(false);
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return retry.Id;
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<BreedSupplyRow>> GetBreedsWithSupplyAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Breeds
            .AsNoTracking()
            .Include(b => b.Supply)
            .OrderBy(b => b.Name)
            .Select(b => new BreedSupplyRow(
                b.Id,
                b.Name,
                b.Category.ToString(),
                b.LifespanYears,
                b.BaselineDesirability,
                b.MaintenanceCost,
                b.RetailPrice,
                b.Supply!.RemainingCount))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PurchasePetsResult?> PurchasePetsAsync(
        Guid traderId,
        Guid breedId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return null;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var breed = await _db.Breeds
            .Include(b => b.Supply)
            .FirstOrDefaultAsync(b => b.Id == breedId, cancellationToken)
            .ConfigureAwait(false);
        if (breed?.Supply is null)
        {
            return null;
        }

        var trader = await _db.Traders.FirstOrDefaultAsync(t => t.Id == traderId, cancellationToken).ConfigureAwait(false);
        if (trader is null)
        {
            return null;
        }

        var totalCost = breed.RetailPrice * quantity;
        if (breed.Supply.RemainingCount < quantity)
        {
            return null;
        }

        if (!await TryDebitSpendableCashAsync(trader, totalCost, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        breed.Supply.RemainingCount -= quantity;
        var now = DateTimeOffset.UtcNow;
        var pets = new List<Pet>();
        for (var i = 0; i < quantity; i++)
        {
            var pet = new Pet
            {
                Id = Guid.NewGuid(),
                BreedId = breed.Id,
                OwnerTraderId = traderId,
                AgeYears = 0,
                Health = 100m,
                CurrentDesirability = breed.BaselineDesirability,
                IsExpired = false,
                CreatedAt = now
            };
            pets.Add(pet);
            _db.Pets.Add(pet);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        await PublishFundsLinkedAsync(trader, cancellationToken).ConfigureAwait(false);

        var summaries = pets
            .Select(p => ToPetSummary(p, breed))
            .ToList();
        return new PurchasePetsResult(summaries, trader.AvailableCash);
    }

    public async Task<TraderSnapshotRow?> GetTraderSnapshotAsync(Guid traderId, CancellationToken cancellationToken = default)
    {
        var trader = await _db.Traders
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == traderId, cancellationToken)
            .ConfigureAwait(false);
        if (trader is null)
        {
            return null;
        }

        var pets = await _db.Pets
            .AsNoTracking()
            .Include(p => p.Breed)
            .Where(p => p.OwnerTraderId == traderId)
            .OrderBy(p => p.Breed!.Name)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var petRows = pets
            .Where(p => p.Breed is not null)
            .Select(p => ToPetSummary(p, p.Breed!))
            .ToList();

        var portfolioPets = petRows.Sum(p => p.IntrinsicValue);
        var portfolioTotal = trader.AvailableCash + trader.LockedCash + portfolioPets;

        var myBids = await _db.Bids
            .AsNoTracking()
            .Include(b => b.Listing!)
            .ThenInclude(l => l!.Pet)
            .Where(b => b.BuyerTraderId == traderId)
            .OrderByDescending(b => b.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var bidRows = myBids
            .Where(b => b.Listing?.Pet is not null)
            .Select(b => new BidStatusRow(
                b.Id,
                b.ListingId,
                b.Listing!.PetId,
                b.Amount,
                MapBidStatus(b.Status)))
            .ToList();

        return new TraderSnapshotRow(
            trader.Id,
            trader.DisplayName,
            trader.AvailableCash,
            trader.LockedCash,
            portfolioTotal,
            petRows,
            bidRows);
    }

    public async Task<IReadOnlyList<MarketListingRow>> GetMarketListingsAsync(CancellationToken cancellationToken = default)
    {
        var listings = await _db.Listings
            .AsNoTracking()
            .Include(l => l.Pet!)
            .ThenInclude(p => p!.Breed!)
            .ThenInclude(b => b!.Supply)
            .Include(l => l.Seller)
            .Where(l => l.WithdrawnAt == null)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var breedIds = listings.Select(l => l.Pet!.BreedId).Distinct().ToList();
        var recentPrices = await GetRecentTradePricesByBreedAsync(breedIds, cancellationToken).ConfigureAwait(false);

        return listings
            .Where(l => l.Pet?.Breed is not null && l.Seller is not null)
            .Select(l =>
            {
                var breed = l.Pet!.Breed!;
                recentPrices.TryGetValue(breed.Id, out var recent);
                return new MarketListingRow(
                    l.Id,
                    l.PetId,
                    l.SellerTraderId,
                    breed.Name,
                    l.AskingPrice,
                    l.Seller!.DisplayName,
                    l.CreatedAt,
                    recent,
                    breed.Supply?.RemainingCount ?? 0);
            })
            .ToList();
    }

    public async Task<Guid?> CreateListingAsync(
        Guid traderId,
        Guid petId,
        decimal askingPrice,
        CancellationToken cancellationToken = default)
    {
        if (askingPrice <= 0)
        {
            return null;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.Id == petId, cancellationToken).ConfigureAwait(false);
        if (pet is null || pet.OwnerTraderId != traderId)
        {
            return null;
        }

        var active = await _db.Listings.AnyAsync(
            l => l.PetId == petId && l.WithdrawnAt == null,
            cancellationToken)
            .ConfigureAwait(false);
        if (active)
        {
            return null;
        }

        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            PetId = petId,
            SellerTraderId = traderId,
            AskingPrice = askingPrice,
            CreatedAt = DateTimeOffset.UtcNow,
            WithdrawnAt = null
        };
        _db.Listings.Add(listing);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        return listing.Id;
    }

    public async Task<bool> WithdrawListingAsync(Guid traderId, Guid listingId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var listing = await _db.Listings
            .Include(l => l.Pet!)
            .ThenInclude(p => p!.Breed)
            .Include(l => l.Seller)
            .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken)
            .ConfigureAwait(false);
        if (listing is null || listing.SellerTraderId != traderId || listing.WithdrawnAt is not null)
        {
            return false;
        }

        var releasedBuyers = await RejectActiveBidAndReleaseAsync(listing, NotificationType.ListingRemoved, cancellationToken).ConfigureAwait(false);
        listing.WithdrawnAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        foreach (var prior in releasedBuyers)
        {
            await PublishFundsLinkedAsync(prior, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<PlaceBidResult?> PlaceBidAsync(
        Guid traderId,
        Guid listingId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return null;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var listing = await _db.Listings
            .Include(l => l.Pet!)
            .ThenInclude(p => p!.Breed)
            .Include(l => l.Seller)
            .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken)
            .ConfigureAwait(false);
        if (listing is null || listing.WithdrawnAt is not null || listing.Pet is null || listing.Seller is null)
        {
            return null;
        }

        if (listing.SellerTraderId == traderId)
        {
            return null;
        }

        var buyer = await _db.Traders.FirstOrDefaultAsync(t => t.Id == traderId, cancellationToken).ConfigureAwait(false);
        if (buyer is null)
        {
            return null;
        }

        if (amount >= listing.AskingPrice)
        {
            if (!await HasSpendableCashAsync(buyer, amount, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            var supersededBuyers = await SupersedeActivePendingBidsWithNotificationsAsync(listing, cancellationToken).ConfigureAwait(false);
            var trade = await ExecuteTradeAsync(listing, buyer, listing.Seller, amount, cancellationToken).ConfigureAwait(false);
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            await PublishFundsLinkedAsync(buyer, cancellationToken).ConfigureAwait(false);
            await PublishFundsLinkedAsync(listing.Seller, cancellationToken).ConfigureAwait(false);
            foreach (var prior in supersededBuyers)
            {
                await PublishFundsLinkedAsync(prior, cancellationToken).ConfigureAwait(false);
            }

            return new CrossTradePlaceResult(trade);
        }

        if (!await HasSpendableCashAsync(buyer, amount, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var activeBid = await _db.Bids
            .FirstOrDefaultAsync(
                b => b.ListingId == listing.Id && b.Status == BidStatus.Active,
                cancellationToken)
            .ConfigureAwait(false);
        if (activeBid is not null && amount <= activeBid.Amount)
        {
            return null;
        }

        if (activeBid is not null)
        {
            await ReleaseBidLockAsync(activeBid, cancellationToken).ConfigureAwait(false);
            activeBid.Status = BidStatus.Superseded;
            await AddOutbidNotificationAsync(activeBid, listing, cancellationToken).ConfigureAwait(false);
        }

        if (!await TryDebitSpendableCashAsync(buyer, amount, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        buyer.LockedCash += amount;
        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            BuyerTraderId = traderId,
            Amount = amount,
            Status = BidStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Bids.Add(bid);
        await AddBidReceivedNotificationAsync(listing, bid, buyer, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        await PublishFundsLinkedAsync(buyer, cancellationToken).ConfigureAwait(false);
        return new ActiveBidPlaceResult(bid.Id);
    }

    public async Task<bool> WithdrawBidAsync(Guid traderId, Guid bidId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var bid = await _db.Bids
            .Include(b => b.Listing!)
            .ThenInclude(l => l!.Pet)
            .ThenInclude(p => p!.Breed)
            .Include(b => b.Listing!)
            .ThenInclude(l => l!.Seller)
            .Include(b => b.Buyer)
            .FirstOrDefaultAsync(b => b.Id == bidId, cancellationToken)
            .ConfigureAwait(false);
        if (bid is null || bid.BuyerTraderId != traderId || bid.Status != BidStatus.Active || bid.Listing is null)
        {
            return false;
        }

        await ReleaseBidLockAsync(bid, cancellationToken).ConfigureAwait(false);
        bid.Status = BidStatus.Withdrawn;
        await AddBidWithdrawnNotificationAsync(bid, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        if (bid.Buyer is not null)
        {
            await PublishFundsLinkedAsync(bid.Buyer, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<TradeResultRow?> AcceptBidAsync(Guid traderId, Guid listingId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var listing = await _db.Listings
            .Include(l => l.Pet!)
            .ThenInclude(p => p!.Breed)
            .Include(l => l.Seller)
            .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken)
            .ConfigureAwait(false);
        if (listing is null || listing.WithdrawnAt is not null || listing.SellerTraderId != traderId || listing.Pet is null || listing.Seller is null)
        {
            return null;
        }

        var bid = await _db.Bids
            .Include(b => b.Buyer)
            .FirstOrDefaultAsync(
                b => b.ListingId == listing.Id && b.Status == BidStatus.Active,
                cancellationToken)
            .ConfigureAwait(false);
        if (bid is null || bid.Buyer is null || bid.Amount >= listing.AskingPrice)
        {
            return null;
        }

        var buyer = bid.Buyer;
        var seller = listing.Seller;
        buyer.LockedCash -= bid.Amount;
        await CreditSpendableCashAsync(seller, bid.Amount, cancellationToken).ConfigureAwait(false);
        listing.Pet.OwnerTraderId = buyer.Id;
        bid.Status = BidStatus.Accepted;
        listing.WithdrawnAt = DateTimeOffset.UtcNow;
        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            PetId = listing.PetId,
            ListingId = listing.Id,
            BuyerTraderId = buyer.Id,
            SellerTraderId = seller.Id,
            Price = bid.Amount,
            ExecutedAt = DateTimeOffset.UtcNow
        };
        _db.Trades.Add(trade);
        await AddTradeAcceptedNotificationsAsync(listing, trade, buyer, seller, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        await PublishFundsLinkedAsync(seller, cancellationToken).ConfigureAwait(false);
        return new TradeResultRow(trade.Id, trade.PetId, trade.BuyerTraderId, trade.SellerTraderId, trade.Price);
    }

    public async Task<bool> RejectBidAsync(Guid traderId, Guid listingId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var listing = await _db.Listings
            .Include(l => l.Pet!)
            .ThenInclude(p => p!.Breed)
            .Include(l => l.Seller)
            .FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken)
            .ConfigureAwait(false);
        if (listing is null || listing.WithdrawnAt is not null || listing.SellerTraderId != traderId)
        {
            return false;
        }

        var bid = await _db.Bids
            .Include(b => b.Buyer)
            .FirstOrDefaultAsync(
                b => b.ListingId == listing.Id && b.Status == BidStatus.Active,
                cancellationToken)
            .ConfigureAwait(false);
        if (bid is null || bid.Amount >= listing.AskingPrice)
        {
            return false;
        }

        await ReleaseBidLockAsync(bid, cancellationToken).ConfigureAwait(false);
        bid.Status = BidStatus.Rejected;
        await AddBidRejectedNotificationAsync(listing, bid, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        if (bid.Buyer is not null)
        {
            await PublishFundsLinkedAsync(bid.Buyer, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    public async Task<PetAnalysisRow?> GetPetAnalysisAsync(
        Guid petId,
        Guid? viewerTraderId,
        CancellationToken cancellationToken = default)
    {
        var pet = await _db.Pets
            .AsNoTracking()
            .Include(p => p.Breed)
            .FirstOrDefaultAsync(p => p.Id == petId, cancellationToken)
            .ConfigureAwait(false);
        if (pet?.Breed is null)
        {
            return null;
        }

        var listed = await _db.Listings.AnyAsync(
            l => l.PetId == petId && l.WithdrawnAt == null,
            cancellationToken)
            .ConfigureAwait(false);
        var isOwner = viewerTraderId is not null && pet.OwnerTraderId == viewerTraderId;
        if (!listed && !isOwner)
        {
            return null;
        }

        var intrinsic = PetIntrinsicValueCalculator.Calculate(
            pet.Breed.RetailPrice,
            pet.Health,
            pet.CurrentDesirability,
            pet.AgeYears,
            pet.Breed.LifespanYears);

        return new PetAnalysisRow(
            pet.Id,
            pet.Breed.Name,
            pet.AgeYears,
            pet.Health,
            pet.CurrentDesirability,
            pet.Breed.MaintenanceCost,
            intrinsic,
            pet.IsExpired);
    }

    public async Task<IReadOnlyList<LeaderboardRow>> GetLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        var traders = await _db.Traders.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var pets = await _db.Pets
            .AsNoTracking()
            .Include(p => p.Breed)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rows = new List<LeaderboardRow>();
        var rank = 1;
        foreach (var t in traders
                     .Select(tr =>
                     {
                         var owned = pets.Where(p => p.OwnerTraderId == tr.Id && p.Breed is not null).ToList();
                         var value = owned.Sum(p =>
                             PetIntrinsicValueCalculator.Calculate(
                                 p.Breed!.RetailPrice,
                                 p.Health,
                                 p.CurrentDesirability,
                                 p.AgeYears,
                                 p.Breed.LifespanYears));
                         var total = tr.AvailableCash + tr.LockedCash + value;
                         return (tr, total);
                     })
                     .OrderByDescending(x => x.total)
                     .ThenBy(x => x.tr.DisplayName))
        {
            rows.Add(new LeaderboardRow(t.tr.Id, t.tr.DisplayName, t.total, rank++));
        }

        return rows;
    }

    public async Task<IReadOnlyList<NotificationRow>> GetNotificationsAsync(
        Guid traderId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 200);
        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.TraderId == traderId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationRow(
                n.Id,
                n.Type.ToString(),
                n.CreatedAt,
                n.PetId,
                n.PetName,
                n.Amount,
                n.CounterpartyTraderId,
                n.CounterpartyDisplayName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Guid>> RunValuationTickAsync(CancellationToken cancellationToken = default)
    {
        var pets = await _db.Pets
            .Include(p => p.Breed)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (pets.Count == 0)
        {
            return [];
        }

        var seconds = Math.Max(1, _options.ValuationTickSeconds);
        var tickYears = seconds / (365.25m * 24m * 3600m);

        var affected = new List<Guid>();
        foreach (var pet in pets.Where(p => p.Breed is not null))
        {
            var breed = pet.Breed!;
            pet.AgeYears += tickYears;
            var health = pet.Health;
            var desirability = pet.CurrentDesirability;
            ApplyVariance(ref health, ref desirability);
            pet.Health = health;
            pet.CurrentDesirability = desirability;
            pet.IsExpired = pet.AgeYears >= breed.LifespanYears;
            affected.Add(pet.Id);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Trading pets valuation tick updated {Count} pets.", affected.Count);
        return affected;
    }

    private async Task<Dictionary<Guid, decimal>> GetRecentTradePricesByBreedAsync(
        IReadOnlyCollection<Guid> breedIds,
        CancellationToken cancellationToken)
    {
        if (breedIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var trades = await _db.Trades
            .AsNoTracking()
            .Include(t => t.Pet)
            .Where(t => t.Pet != null && breedIds.Contains(t.Pet.BreedId))
            .OrderByDescending(t => t.ExecutedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var map = new Dictionary<Guid, decimal>();
        foreach (var trade in trades)
        {
            if (trade.Pet is null)
            {
                continue;
            }

            var breedId = trade.Pet.BreedId;
            if (!map.ContainsKey(breedId))
            {
                map[breedId] = trade.Price;
            }
        }

        return map;
    }

    private static void ApplyVariance(ref decimal health, ref int desirability)
    {
        var rh = 1 + (decimal)(Random.Shared.NextDouble() * 0.1 - 0.05);
        health = Math.Clamp(Math.Round(health * rh, 2, MidpointRounding.AwayFromZero), 0m, 100m);
        var rd = 1 + (Random.Shared.NextDouble() * 0.1 - 0.05);
        var next = (int)Math.Round(desirability * (decimal)rd, MidpointRounding.AwayFromZero);
        desirability = Math.Clamp(next, 1, 10);
    }

    private static PetSummaryRow ToPetSummary(Pet pet, Breed breed)
    {
        var intrinsic = PetIntrinsicValueCalculator.Calculate(
            breed.RetailPrice,
            pet.Health,
            pet.CurrentDesirability,
            pet.AgeYears,
            breed.LifespanYears);
        return new PetSummaryRow(
            pet.Id,
            breed.Name,
            pet.AgeYears,
            pet.Health,
            pet.CurrentDesirability,
            intrinsic,
            pet.IsExpired,
            breed.MaintenanceCost);
    }

    private static string MapBidStatus(BidStatus status) =>
        status switch
        {
            BidStatus.Superseded => "Outbid",
            BidStatus.Active => "Active",
            BidStatus.Withdrawn => "Withdrawn",
            BidStatus.Rejected => "Rejected",
            BidStatus.Accepted => "Accepted",
            _ => status.ToString()
        };

    private async Task<IReadOnlyList<Trader>> SupersedeActivePendingBidsWithNotificationsAsync(
        Listing listing,
        CancellationToken cancellationToken)
    {
        var releasedBuyers = new List<Trader>();
        var active = await _db.Bids
            .Include(b => b.Buyer)
            .Where(b => b.ListingId == listing.Id && b.Status == BidStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var b in active)
        {
            await ReleaseBidLockAsync(b, cancellationToken).ConfigureAwait(false);
            b.Status = BidStatus.Superseded;
            if (b.Buyer is not null)
            {
                releasedBuyers.Add(b.Buyer);
                await AddNotificationAsync(
                    b.Buyer.Id,
                    NotificationType.Outbid,
                    listing.PetId,
                    PetDisplayName(listing.Pet),
                    b.Amount,
                    listing.SellerTraderId,
                    listing.Seller?.DisplayName ?? "Seller",
                    b.Id.ToString(),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return releasedBuyers;
    }

    private async Task ReleaseBidLockAsync(Bid bid, CancellationToken cancellationToken)
    {
        var buyer = await _db.Traders.FirstOrDefaultAsync(t => t.Id == bid.BuyerTraderId, cancellationToken).ConfigureAwait(false);
        if (buyer is null)
        {
            return;
        }

        buyer.LockedCash -= bid.Amount;
        await CreditSpendableCashAsync(buyer, bid.Amount, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<Trader>> RejectActiveBidAndReleaseAsync(
        Listing listing,
        NotificationType typeForBuyer,
        CancellationToken cancellationToken)
    {
        var releasedBuyers = new List<Trader>();
        var active = await _db.Bids
            .Include(b => b.Buyer)
            .Where(b => b.ListingId == listing.Id && b.Status == BidStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var bid in active)
        {
            await ReleaseBidLockAsync(bid, cancellationToken).ConfigureAwait(false);
            bid.Status = BidStatus.Rejected;
            if (bid.Buyer is not null)
            {
                releasedBuyers.Add(bid.Buyer);
                if (listing.Seller is not null)
                {
                    await AddNotificationAsync(
                        bid.Buyer.Id,
                        typeForBuyer,
                        listing.PetId,
                        PetDisplayName(listing.Pet),
                        bid.Amount,
                        listing.Seller.Id,
                        listing.Seller.DisplayName,
                        listing.Id.ToString(),
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return releasedBuyers;
    }

    private async Task<TradeResultRow> ExecuteTradeAsync(
        Listing listing,
        Trader buyer,
        Trader seller,
        decimal price,
        CancellationToken cancellationToken)
    {
        if (!await TryDebitSpendableCashAsync(buyer, price, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Buyer cannot fund trade.");
        }

        await CreditSpendableCashAsync(seller, price, cancellationToken).ConfigureAwait(false);
        listing.Pet!.OwnerTraderId = buyer.Id;
        listing.WithdrawnAt = DateTimeOffset.UtcNow;
        var trade = new Trade
        {
            Id = Guid.NewGuid(),
            PetId = listing.PetId,
            ListingId = listing.Id,
            BuyerTraderId = buyer.Id,
            SellerTraderId = seller.Id,
            Price = price,
            ExecutedAt = DateTimeOffset.UtcNow
        };
        _db.Trades.Add(trade);
        await AddCrossTradeNotificationsAsync(listing, trade, buyer, seller, cancellationToken).ConfigureAwait(false);
        return new TradeResultRow(trade.Id, trade.PetId, trade.BuyerTraderId, trade.SellerTraderId, trade.Price);
    }

    private static string PetDisplayName(Pet? pet)
    {
        if (pet?.Breed is null)
        {
            return "Pet";
        }

        var id = pet.Id.ToString("N");
        var shortId = id.Length >= 8 ? id[..8] : id;
        return $"{pet.Breed.Name} ({shortId})";
    }

    private Task AddBidReceivedNotificationAsync(
        Listing listing,
        Bid bid,
        Trader buyer,
        CancellationToken cancellationToken) =>
        AddNotificationAsync(
            listing.SellerTraderId,
            NotificationType.BidReceived,
            listing.PetId,
            PetDisplayName(listing.Pet),
            bid.Amount,
            bid.BuyerTraderId,
            buyer.DisplayName,
            bid.Id.ToString(),
            cancellationToken);

    private async Task AddOutbidNotificationAsync(Bid prior, Listing listing, CancellationToken cancellationToken)
    {
        var buyer = prior.Buyer ?? await _db.Traders.FirstAsync(t => t.Id == prior.BuyerTraderId, cancellationToken).ConfigureAwait(false);
        await AddNotificationAsync(
            buyer.Id,
            NotificationType.Outbid,
            listing.PetId,
            PetDisplayName(listing.Pet),
            prior.Amount,
            listing.SellerTraderId,
            listing.Seller?.DisplayName ?? "Seller",
            prior.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task AddBidWithdrawnNotificationAsync(Bid bid, CancellationToken cancellationToken)
    {
        if (bid.Listing?.Seller is null)
        {
            return;
        }

        await AddNotificationAsync(
            bid.Listing.SellerTraderId,
            NotificationType.BidWithdrawn,
            bid.Listing.PetId,
            PetDisplayName(bid.Listing.Pet),
            bid.Amount,
            bid.BuyerTraderId,
            bid.Buyer?.DisplayName ?? "Buyer",
            bid.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    private Task AddBidRejectedNotificationAsync(Listing listing, Bid bid, CancellationToken cancellationToken) =>
        AddNotificationAsync(
            bid.BuyerTraderId,
            NotificationType.BidRejected,
            listing.PetId,
            PetDisplayName(listing.Pet),
            bid.Amount,
            listing.SellerTraderId,
            listing.Seller?.DisplayName ?? "Seller",
            bid.Id.ToString(),
            cancellationToken);

    private async Task AddTradeAcceptedNotificationsAsync(
        Listing listing,
        Trade trade,
        Trader buyer,
        Trader seller,
        CancellationToken cancellationToken)
    {
        await AddNotificationAsync(
            seller.Id,
            NotificationType.BidAccepted,
            trade.PetId,
            PetDisplayName(listing.Pet),
            trade.Price,
            buyer.Id,
            buyer.DisplayName,
            trade.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
        await AddNotificationAsync(
            buyer.Id,
            NotificationType.BidAccepted,
            trade.PetId,
            PetDisplayName(listing.Pet),
            trade.Price,
            seller.Id,
            seller.DisplayName,
            trade.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task AddCrossTradeNotificationsAsync(
        Listing listing,
        Trade trade,
        Trader buyer,
        Trader seller,
        CancellationToken cancellationToken)
    {
        await AddNotificationAsync(
            seller.Id,
            NotificationType.TradeCompleted,
            trade.PetId,
            PetDisplayName(listing.Pet),
            trade.Price,
            buyer.Id,
            buyer.DisplayName,
            trade.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
        await AddNotificationAsync(
            buyer.Id,
            NotificationType.TradeCompleted,
            trade.PetId,
            PetDisplayName(listing.Pet),
            trade.Price,
            seller.Id,
            seller.DisplayName,
            trade.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task AddNotificationAsync(
        Guid recipientTraderId,
        NotificationType type,
        Guid petId,
        string petName,
        decimal? amount,
        Guid counterpartyId,
        string counterpartyName,
        string? correlation,
        CancellationToken cancellationToken)
    {
        _db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            TraderId = recipientTraderId,
            Type = type,
            PetId = petId,
            PetName = petName,
            Amount = amount,
            CounterpartyTraderId = counterpartyId,
            CounterpartyDisplayName = counterpartyName,
            CreatedAt = DateTimeOffset.UtcNow,
            Correlation = correlation
        });
        await Task.CompletedTask;
    }

    private static bool IsLinkedToDemoAccount(Trader trader) =>
        !string.IsNullOrWhiteSpace(trader.ExternalUserId);

    private async Task<bool> HasSpendableCashAsync(Trader trader, decimal amount, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (!IsLinkedToDemoAccount(trader))
        {
            return trader.AvailableCash >= amount;
        }

        var acct = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == trader.ExternalUserId!, cancellationToken)
            .ConfigureAwait(false);
        return acct is not null && acct.CashAvailable >= amount;
    }

    private async Task<bool> TryDebitSpendableCashAsync(Trader trader, decimal amount, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (!IsLinkedToDemoAccount(trader))
        {
            if (trader.AvailableCash < amount)
            {
                return false;
            }

            trader.AvailableCash -= amount;
            return true;
        }

        var acct = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == trader.ExternalUserId!, cancellationToken)
            .ConfigureAwait(false);
        if (acct is null || acct.CashAvailable < amount)
        {
            return false;
        }

        acct.CashAvailable -= amount;
        trader.AvailableCash -= amount;
        return true;
    }

    private async Task CreditSpendableCashAsync(Trader trader, decimal amount, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return;
        }

        trader.AvailableCash += amount;
        if (!IsLinkedToDemoAccount(trader))
        {
            return;
        }

        var acct = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == trader.ExternalUserId!, cancellationToken)
            .ConfigureAwait(false);
        if (acct is null)
        {
            return;
        }

        acct.CashAvailable += amount;
    }

    private async Task ReconcileLinkedTraderAvailableFromDemoAccountAsync(Trader trader, CancellationToken cancellationToken)
    {
        if (!IsLinkedToDemoAccount(trader) || trader.LockedCash != 0)
        {
            return;
        }

        var acct = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == trader.ExternalUserId!, cancellationToken)
            .ConfigureAwait(false);
        if (acct is null)
        {
            return;
        }

        if (trader.AvailableCash != acct.CashAvailable)
        {
            trader.AvailableCash = acct.CashAvailable;
        }
    }

    private async Task PublishFundsLinkedAsync(Trader? trader, CancellationToken cancellationToken)
    {
        if (trader is null || !IsLinkedToDemoAccount(trader) || _fundsRealtime is null)
        {
            return;
        }

        var acct = await _db.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == trader.ExternalUserId!, cancellationToken)
            .ConfigureAwait(false);
        if (acct is null)
        {
            return;
        }

        await _fundsRealtime.NotifyFundsUpdatedAsync(
            new FundsUpdatedRealtimeDto(acct.UserId, acct.CashAvailable, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
    }
}
