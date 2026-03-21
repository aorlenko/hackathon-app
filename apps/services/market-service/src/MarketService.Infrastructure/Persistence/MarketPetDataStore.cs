using MarketService.Application.Abstractions;
using MarketService.Application.Accounts;
using MarketService.Application.Pets;
using MarketService.Application.Realtime;
using MarketService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Trading.Contracts.Events;
using Trading.Contracts.Http;

namespace MarketService.Infrastructure.Persistence;

public sealed class MarketPetDataStore : IMarketPetStore
{
    private readonly MarketDbContext _db;
    private readonly ILogger<MarketPetDataStore> _logger;
    private readonly TradingPetsOptions _options;
    private readonly IMarketRealtimeNotifier? _fundsRealtime;
    private readonly ILifecycleEventPublisher? _lifecycle;

    public MarketPetDataStore(
        MarketDbContext db,
        ILogger<MarketPetDataStore> logger,
        IOptions<TradingPetsOptions> options,
        IMarketRealtimeNotifier? fundsRealtime = null,
        ILifecycleEventPublisher? lifecycle = null)
    {
        _db = db;
        _logger = logger;
        _options = options.Value;
        _fundsRealtime = fundsRealtime;
        _lifecycle = lifecycle;
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
        string? email = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUserSub);

        var normalizedName = string.IsNullOrWhiteSpace(displayName) ? "Trader" : displayName.Trim();

        await _db.EnsureDemoAccountAsync(externalUserSub, normalizedName, email, cancellationToken).ConfigureAwait(false);

        var existing = await _db.Traders
            .FirstOrDefaultAsync(t => t.ExternalUserId == externalUserSub, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return existing.Id;
        }

        var trader = new Trader
        {
            Id = Guid.NewGuid(),
            DisplayName = normalizedName,
            ExternalUserId = externalUserSub,
            AvailableCash = _options.InitialTraderCash,
            LockedCash = 0
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
                IsExpired = false
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

        var snapshotDisplayName = trader.DisplayName;
        if (!string.IsNullOrWhiteSpace(trader.ExternalUserId))
        {
            var acct = await _db.Accounts
                .AsNoTracking()
                .Where(a => a.UserId == trader.ExternalUserId)
                .Select(a => new { a.DisplayName, a.Email })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (acct is not null)
            {
                snapshotDisplayName = AccountPublicDisplayName.ForLinkedExternalUser(
                    trader.DisplayName,
                    acct.DisplayName,
                    acct.Email);
            }
        }
        else
        {
            snapshotDisplayName = AccountPublicDisplayName.ForUnlinkedTrader(trader.DisplayName);
        }

        return new TraderSnapshotRow(
            trader.Id,
            snapshotDisplayName,
            trader.AvailableCash,
            trader.LockedCash,
            portfolioTotal,
            petRows,
            bidRows);
    }

    public async Task<IReadOnlyList<MarketListingRow>> GetMarketListingsAsync(
        Guid? viewerTraderId = null,
        CancellationToken cancellationToken = default)
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

        var sellerUserIds = listings
            .Select(l => l.Seller?.ExternalUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        Dictionary<string, (string DisplayName, string Email)> sellerAccountByUserId;
        if (sellerUserIds.Count == 0)
        {
            sellerAccountByUserId = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        }
        else
        {
            var accountRows = await _db.Accounts.AsNoTracking()
                .Where(a => sellerUserIds.Contains(a.UserId))
                .Select(a => new { a.UserId, a.DisplayName, a.Email })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            sellerAccountByUserId = accountRows.ToDictionary(
                x => x.UserId,
                x => (x.DisplayName, x.Email),
                StringComparer.Ordinal);
        }

        var rows = listings
            .Where(l => l.Pet?.Breed is not null && l.Seller is not null)
            .Select(l =>
            {
                var breed = l.Pet!.Breed!;
                recentPrices.TryGetValue(breed.Id, out var recent);
                string? sellerEmail = null;
                var ext = l.Seller!.ExternalUserId;
                var traderDisplay = l.Seller!.DisplayName.Trim();
                var sellerDisplayName = traderDisplay;
                if (!string.IsNullOrWhiteSpace(ext) && sellerAccountByUserId.TryGetValue(ext, out var acct))
                {
                    var acctEmail = acct.Email.Trim();
                    if (acctEmail.Length > 0)
                    {
                        sellerEmail = acctEmail;
                    }

                    sellerDisplayName = AccountPublicDisplayName.ForLinkedExternalUser(
                        traderDisplay,
                        acct.DisplayName,
                        acct.Email);
                }
                else
                {
                    sellerDisplayName = AccountPublicDisplayName.ForUnlinkedTrader(traderDisplay);
                }

                return new MarketListingRow(
                    l.Id,
                    l.PetId,
                    l.SellerTraderId,
                    breed.Name,
                    l.AskingPrice,
                    sellerDisplayName,
                    sellerEmail,
                    l.CreatedAt,
                    recent,
                    breed.Supply?.RemainingCount ?? 0,
                    null,
                    null);
            })
            .ToList();

        if (!viewerTraderId.HasValue)
        {
            return rows;
        }

        var myListingIds = rows
            .Where(r => r.SellerTraderId == viewerTraderId.Value)
            .Select(r => r.ListingId)
            .ToList();
        if (myListingIds.Count == 0)
        {
            return rows;
        }

        var activeBids = await _db.Bids
            .AsNoTracking()
            .Include(b => b.Buyer)
            .Where(b => myListingIds.Contains(b.ListingId) && b.Status == BidStatus.Active)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var sellerBidByListing = new Dictionary<Guid, (decimal Amount, string BuyerLabel)>();
        foreach (var bid in activeBids)
        {
            if (bid.Buyer is null)
            {
                continue;
            }

            var label = await GetPublicTraderLabelAsync(bid.Buyer, cancellationToken).ConfigureAwait(false);
            sellerBidByListing[bid.ListingId] = (bid.Amount, label);
        }

        return rows
            .Select(r =>
                sellerBidByListing.TryGetValue(r.ListingId, out var bid)
                    ? r with
                    {
                        ActiveBidAmount = bid.Amount,
                        ActiveBidBuyerDisplayName = bid.BuyerLabel,
                    }
                    : r)
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

            await PublishPeerTradeOrderMatchedAsync(
                trade.TradeId,
                trade.PetId,
                PeerTradeSymbol(listing.Pet),
                trade.Price,
                trade.ExecutedAt,
                buyer.ExternalUserId,
                listing.Seller.ExternalUserId,
                cancellationToken).ConfigureAwait(false);

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
            Status = BidStatus.Active
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
        await PublishPeerTradeOrderMatchedAsync(
            trade.Id,
            trade.PetId,
            PeerTradeSymbol(listing.Pet),
            trade.Price,
            trade.ExecutedAt,
            buyer.ExternalUserId,
            seller.ExternalUserId,
            cancellationToken).ConfigureAwait(false);

        return new TradeResultRow(
            trade.Id,
            trade.PetId,
            trade.BuyerTraderId,
            trade.SellerTraderId,
            trade.Price,
            trade.ExecutedAt);
    }

    public async Task<(bool ok, Guid? buyerTraderId)> RejectBidAsync(
        Guid traderId,
        Guid listingId,
        CancellationToken cancellationToken = default)
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
            return (false, null);
        }

        var bid = await _db.Bids
            .Include(b => b.Buyer)
            .FirstOrDefaultAsync(
                b => b.ListingId == listing.Id && b.Status == BidStatus.Active,
                cancellationToken)
            .ConfigureAwait(false);
        if (bid is null || bid.Amount >= listing.AskingPrice)
        {
            return (false, null);
        }

        await ReleaseBidLockAsync(bid, cancellationToken).ConfigureAwait(false);
        bid.Status = BidStatus.Rejected;
        await AddBidRejectedNotificationAsync(listing, bid, cancellationToken).ConfigureAwait(false);
        await AddSellerRejectedBidNotificationAsync(listing, bid, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        if (bid.Buyer is not null)
        {
            await PublishFundsLinkedAsync(bid.Buyer, cancellationToken).ConfigureAwait(false);
        }

        return (true, bid.BuyerTraderId);
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
        var leaderboardExtIds = traders
            .Select(tr => tr.ExternalUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        Dictionary<string, (string DisplayName, string Email)> leaderboardAccounts;
        if (leaderboardExtIds.Count == 0)
        {
            leaderboardAccounts = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        }
        else
        {
            var acctRows = await _db.Accounts
                .AsNoTracking()
                .Where(a => leaderboardExtIds.Contains(a.UserId))
                .Select(a => new { a.UserId, a.DisplayName, a.Email })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            leaderboardAccounts = acctRows.ToDictionary(
                x => x.UserId,
                x => (x.DisplayName, x.Email),
                StringComparer.Ordinal);
        }

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
            var label = string.IsNullOrWhiteSpace(t.tr.ExternalUserId)
                ? AccountPublicDisplayName.ForUnlinkedTrader(t.tr.DisplayName)
                : leaderboardAccounts.TryGetValue(t.tr.ExternalUserId!, out var acct)
                    ? AccountPublicDisplayName.ForLinkedExternalUser(t.tr.DisplayName, acct.DisplayName, acct.Email)
                    : AccountPublicDisplayName.ForUnlinkedTrader(t.tr.DisplayName);
            rows.Add(new LeaderboardRow(t.tr.Id, label, t.total, rank++));
        }

        return rows;
    }

    public async Task<IReadOnlyList<PetResaleTradeHistoryRow>> GetResaleTradesForExternalUserAsync(
        string externalUserSub,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalUserSub))
        {
            return [];
        }

        const int maxRows = 200;
        var trades = await _db.Trades
            .AsNoTracking()
            .Include(t => t.Buyer)
            .Include(t => t.Seller)
            .Include(t => t.Pet!)
            .ThenInclude(p => p.Breed)
            .Where(t =>
                t.Buyer != null
                && t.Seller != null
                && (t.Buyer.ExternalUserId == externalUserSub || t.Seller.ExternalUserId == externalUserSub))
            .OrderByDescending(t => t.ExecutedAt)
            .Take(maxRows)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var list = new List<PetResaleTradeHistoryRow>(trades.Count);
        foreach (var t in trades)
        {
            var breedName = t.Pet?.Breed?.Name;
            var symbol = string.IsNullOrWhiteSpace(breedName) ? "PET_RESALE" : $"PET/{breedName}";
            list.Add(
                new PetResaleTradeHistoryRow(
                    t.Id,
                    symbol,
                    t.Price,
                    1,
                    t.ExecutedAt,
                    t.Buyer!.ExternalUserId ?? string.Empty,
                    t.Seller!.ExternalUserId ?? string.Empty));
        }

        return list;
    }

    public async Task<IReadOnlyList<NotificationRow>> GetNotificationsAsync(
        Guid traderId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 200);
        var raw = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.TraderId == traderId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new
            {
                n.Id,
                n.Type,
                n.CreatedAt,
                n.PetId,
                n.PetName,
                n.Amount,
                n.CounterpartyTraderId
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (raw.Count == 0)
        {
            return [];
        }

        var counterpartyIds = raw.Select(r => r.CounterpartyTraderId).Distinct().ToList();
        var counterpartyTraders = await _db.Traders
            .AsNoTracking()
            .Where(t => counterpartyIds.Contains(t.Id))
            .Select(t => new { t.Id, t.DisplayName, t.ExternalUserId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var counterpartyExtIds = counterpartyTraders
            .Select(t => t.ExternalUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();
        Dictionary<string, (string DisplayName, string Email)> counterpartyAccounts;
        if (counterpartyExtIds.Count == 0)
        {
            counterpartyAccounts = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        }
        else
        {
            var rows = await _db.Accounts
                .AsNoTracking()
                .Where(a => counterpartyExtIds.Contains(a.UserId))
                .Select(a => new { a.UserId, a.DisplayName, a.Email })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            counterpartyAccounts = rows.ToDictionary(
                x => x.UserId,
                x => (x.DisplayName, x.Email),
                StringComparer.Ordinal);
        }

        var idToTrader = counterpartyTraders.ToDictionary(x => x.Id);
        var labels = new Dictionary<Guid, string>();
        foreach (var id in counterpartyIds)
        {
            if (!idToTrader.TryGetValue(id, out var ct))
            {
                labels[id] = "Trader";
                continue;
            }

            if (string.IsNullOrWhiteSpace(ct.ExternalUserId))
            {
                labels[id] = AccountPublicDisplayName.ForUnlinkedTrader(ct.DisplayName);
                continue;
            }

            if (!counterpartyAccounts.TryGetValue(ct.ExternalUserId!, out var acct))
            {
                labels[id] = AccountPublicDisplayName.ForUnlinkedTrader(ct.DisplayName);
                continue;
            }

            labels[id] = AccountPublicDisplayName.ForLinkedExternalUser(ct.DisplayName, acct.DisplayName, acct.Email);
        }

        return raw
            .Select(n => new NotificationRow(
                n.Id,
                n.Type.ToString(),
                n.CreatedAt,
                n.PetId,
                n.PetName,
                n.Amount,
                n.CounterpartyTraderId,
                labels[n.CounterpartyTraderId]))
            .ToList();
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

        var tickYears = _options.AgeYearsEveryMinute > 0m
            ? _options.AgeYearsEveryMinute
            : TradingPetsOptions.DefaultAgeYearsEveryMinute;

        var affected = new List<Guid>();
        foreach (var pet in pets.Where(p => p.Breed is not null && !p.IsExpired))
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

    private async Task<string> GetPublicTraderLabelAsync(Trader trader, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(trader.ExternalUserId))
        {
            return AccountPublicDisplayName.ForUnlinkedTrader(trader.DisplayName);
        }

        var acct = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == trader.ExternalUserId, cancellationToken)
            .ConfigureAwait(false);
        if (acct is null)
        {
            return AccountPublicDisplayName.ForUnlinkedTrader(trader.DisplayName);
        }

        return AccountPublicDisplayName.ForLinkedExternalUser(trader.DisplayName, acct.DisplayName, acct.Email);
    }

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
        var sellerLabel = listing.Seller is not null
            ? await GetPublicTraderLabelAsync(listing.Seller, cancellationToken).ConfigureAwait(false)
            : "Seller";
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
                    sellerLabel,
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
        var sellerLabel = listing.Seller is not null
            ? await GetPublicTraderLabelAsync(listing.Seller, cancellationToken).ConfigureAwait(false)
            : "Seller";
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
                        sellerLabel,
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
        return new TradeResultRow(
            trade.Id,
            trade.PetId,
            trade.BuyerTraderId,
            trade.SellerTraderId,
            trade.Price,
            trade.ExecutedAt);
    }

    private static string PeerTradeSymbol(Pet? pet)
    {
        var breed = pet?.Breed?.Name;
        return string.IsNullOrWhiteSpace(breed) ? "PET_RESALE" : $"PET/{breed.Trim()}";
    }

    private async Task PublishPeerTradeOrderMatchedAsync(
        Guid tradeId,
        Guid petId,
        string symbol,
        decimal price,
        DateTimeOffset executedAt,
        string? buyerUserId,
        string? sellerUserId,
        CancellationToken cancellationToken)
    {
        if (_lifecycle is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(buyerUserId) || string.IsNullOrWhiteSpace(sellerUserId))
        {
            _logger.LogWarning(
                "Peer trade {TradeId} committed but lifecycle event was skipped (buyer or seller has no ExternalUserId).",
                tradeId);
            return;
        }

        var correlationId = tradeId.ToString("N");
        var buyOrderId = DerivePeerSyntheticOrderId(tradeId, 0x01);
        var sellOrderId = DerivePeerSyntheticOrderId(tradeId, 0x02);
        var evt = new OrderMatched(
            Guid.NewGuid(),
            executedAt,
            correlationId,
            "market-service",
            tradeId,
            buyOrderId,
            sellOrderId,
            buyerUserId.Trim(),
            sellerUserId.Trim(),
            petId,
            symbol,
            price,
            1,
            executedAt);

        try
        {
            await _lifecycle.PublishAsync(evt, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OrderMatched for peer trade {TradeId}.", tradeId);
        }
    }

    private static Guid DerivePeerSyntheticOrderId(Guid tradeId, byte tag)
    {
        Span<byte> bytes = stackalloc byte[16];
        tradeId.TryWriteBytes(bytes);
        bytes[15] ^= tag;
        return new Guid(bytes);
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

    private async Task AddBidReceivedNotificationAsync(
        Listing listing,
        Bid bid,
        Trader buyer,
        CancellationToken cancellationToken)
    {
        var buyerLabel = await GetPublicTraderLabelAsync(buyer, cancellationToken).ConfigureAwait(false);
        await AddNotificationAsync(
            listing.SellerTraderId,
            NotificationType.BidReceived,
            listing.PetId,
            PetDisplayName(listing.Pet),
            bid.Amount,
            bid.BuyerTraderId,
            buyerLabel,
            bid.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task AddOutbidNotificationAsync(Bid prior, Listing listing, CancellationToken cancellationToken)
    {
        var buyer = prior.Buyer ?? await _db.Traders.FirstAsync(t => t.Id == prior.BuyerTraderId, cancellationToken).ConfigureAwait(false);
        var sellerLabel = listing.Seller is not null
            ? await GetPublicTraderLabelAsync(listing.Seller, cancellationToken).ConfigureAwait(false)
            : "Seller";
        await AddNotificationAsync(
            buyer.Id,
            NotificationType.Outbid,
            listing.PetId,
            PetDisplayName(listing.Pet),
            prior.Amount,
            listing.SellerTraderId,
            sellerLabel,
            prior.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task AddBidWithdrawnNotificationAsync(Bid bid, CancellationToken cancellationToken)
    {
        if (bid.Listing?.Seller is null)
        {
            return;
        }

        var buyer = bid.Buyer ?? await _db.Traders
            .FirstOrDefaultAsync(t => t.Id == bid.BuyerTraderId, cancellationToken)
            .ConfigureAwait(false);
        if (buyer is null)
        {
            return;
        }

        var buyerLabel = await GetPublicTraderLabelAsync(buyer, cancellationToken).ConfigureAwait(false);
        await AddNotificationAsync(
            bid.Listing.SellerTraderId,
            NotificationType.BidWithdrawn,
            bid.Listing.PetId,
            PetDisplayName(bid.Listing.Pet),
            bid.Amount,
            bid.BuyerTraderId,
            buyerLabel,
            bid.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task AddBidRejectedNotificationAsync(Listing listing, Bid bid, CancellationToken cancellationToken)
    {
        var sellerLabel = listing.Seller is not null
            ? await GetPublicTraderLabelAsync(listing.Seller, cancellationToken).ConfigureAwait(false)
            : "Seller";
        await AddNotificationAsync(
            bid.BuyerTraderId,
            NotificationType.BidRejected,
            listing.PetId,
            PetDisplayName(listing.Pet),
            bid.Amount,
            listing.SellerTraderId,
            sellerLabel,
            bid.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task AddSellerRejectedBidNotificationAsync(Listing listing, Bid bid, CancellationToken cancellationToken)
    {
        if (listing.Seller is null)
        {
            return;
        }

        var buyer = bid.Buyer ?? await _db.Traders
            .FirstOrDefaultAsync(t => t.Id == bid.BuyerTraderId, cancellationToken)
            .ConfigureAwait(false);
        if (buyer is null)
        {
            return;
        }

        var buyerLabel = await GetPublicTraderLabelAsync(buyer, cancellationToken).ConfigureAwait(false);
        await AddNotificationAsync(
            listing.SellerTraderId,
            NotificationType.SellerRejectedBid,
            listing.PetId,
            PetDisplayName(listing.Pet),
            bid.Amount,
            bid.BuyerTraderId,
            buyerLabel,
            bid.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task AddTradeAcceptedNotificationsAsync(
        Listing listing,
        Trade trade,
        Trader buyer,
        Trader seller,
        CancellationToken cancellationToken)
    {
        var buyerLabel = await GetPublicTraderLabelAsync(buyer, cancellationToken).ConfigureAwait(false);
        var sellerLabel = await GetPublicTraderLabelAsync(seller, cancellationToken).ConfigureAwait(false);
        await AddNotificationAsync(
            seller.Id,
            NotificationType.BidAccepted,
            trade.PetId,
            PetDisplayName(listing.Pet),
            trade.Price,
            buyer.Id,
            buyerLabel,
            trade.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
        await AddNotificationAsync(
            buyer.Id,
            NotificationType.BidAccepted,
            trade.PetId,
            PetDisplayName(listing.Pet),
            trade.Price,
            seller.Id,
            sellerLabel,
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
        var buyerLabel = await GetPublicTraderLabelAsync(buyer, cancellationToken).ConfigureAwait(false);
        var sellerLabel = await GetPublicTraderLabelAsync(seller, cancellationToken).ConfigureAwait(false);
        await AddNotificationAsync(
            seller.Id,
            NotificationType.TradeCompleted,
            trade.PetId,
            PetDisplayName(listing.Pet),
            trade.Price,
            buyer.Id,
            buyerLabel,
            trade.Id.ToString(),
            cancellationToken).ConfigureAwait(false);
        await AddNotificationAsync(
            buyer.Id,
            NotificationType.TradeCompleted,
            trade.PetId,
            PetDisplayName(listing.Pet),
            trade.Price,
            seller.Id,
            sellerLabel,
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
            Correlation = correlation
        });
        await Task.CompletedTask;
    }

    private static bool HasExternalUser(Trader trader) =>
        !string.IsNullOrWhiteSpace(trader.ExternalUserId);

    private static Task<bool> HasSpendableCashAsync(Trader trader, decimal amount, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(trader.AvailableCash >= amount);
    }

    private static Task<bool> TryDebitSpendableCashAsync(Trader trader, decimal amount, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return Task.FromResult(true);
        }

        if (trader.AvailableCash < amount)
        {
            return Task.FromResult(false);
        }

        trader.AvailableCash -= amount;
        return Task.FromResult(true);
    }

    private static Task CreditSpendableCashAsync(Trader trader, decimal amount, CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            return Task.CompletedTask;
        }

        trader.AvailableCash += amount;
        return Task.CompletedTask;
    }

    private async Task PublishFundsLinkedAsync(Trader? trader, CancellationToken cancellationToken)
    {
        if (trader is null || !HasExternalUser(trader) || _fundsRealtime is null)
        {
            return;
        }

        await _fundsRealtime.NotifyFundsUpdatedAsync(
            new FundsUpdatedRealtimeDto(trader.ExternalUserId!, trader.AvailableCash, DateTimeOffset.UtcNow),
            cancellationToken).ConfigureAwait(false);
    }
}
