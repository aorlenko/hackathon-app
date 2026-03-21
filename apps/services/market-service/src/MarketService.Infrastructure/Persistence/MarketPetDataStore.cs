using MarketService.Application.Abstractions;
using MarketService.Application.Pets;
using MarketService.Application.Pets.Terminal;
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
            AvailableCash = MarketSeedData.AutoProvisionedCashAvailable,
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

        return listings
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

                    var acctName = acct.DisplayName.Trim();
                    if (traderDisplay.Contains('|', StringComparison.Ordinal) && acctName.Length > 0)
                    {
                        sellerDisplayName = acctName;
                    }
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
                    breed.Supply?.RemainingCount ?? 0);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<TerminalMarketRow>> GetTerminalMarketsAsync(CancellationToken cancellationToken = default)
    {
        var breeds = await _db.Breeds
            .AsNoTracking()
            .Include(b => b.Supply)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var marketData = await BuildTerminalMarketDataAsync(cancellationToken).ConfigureAwait(false);

        return breeds
            .Select(breed =>
            {
                marketData.TryGetValue(breed.Id, out var projection);
                return new TerminalMarketRow(
                    breed.Id,
                    breed.Name,
                    breed.Supply?.RemainingCount ?? 0,
                    projection?.LatestTradePrice,
                    projection?.BestBidPrice,
                    projection?.BestAskPrice,
                    projection?.TrendDirection ?? TerminalTrendDirection.NoTradeData,
                    projection?.LastTradeAt);
            })
            .ToList();
    }

    public async Task<TerminalWorkspaceSnapshot?> GetTerminalWorkspaceAsync(
        Guid traderId,
        Guid marketEntryId,
        CancellationToken cancellationToken = default)
    {
        var trader = await _db.Traders
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == traderId, cancellationToken)
            .ConfigureAwait(false);
        if (trader is null)
        {
            return null;
        }

        var marketEntry = (await GetTerminalMarketsAsync(cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(row => row.MarketEntryId == marketEntryId);
        if (marketEntry is null)
        {
            return null;
        }

        var activeListings = await _db.Listings
            .AsNoTracking()
            .Include(l => l.Pet)
            .Where(l => l.WithdrawnAt == null && l.Pet != null && l.Pet.BreedId == marketEntryId)
            .OrderBy(l => l.AskingPrice)
            .ThenBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var activeBids = await _db.Bids
            .AsNoTracking()
            .Include(b => b.Listing!)
            .ThenInclude(l => l!.Pet)
            .Where(b =>
                b.Status == BidStatus.Active &&
                b.Listing != null &&
                b.Listing.WithdrawnAt == null &&
                b.Listing.Pet != null &&
                b.Listing.Pet.BreedId == marketEntryId &&
                b.Amount < b.Listing.AskingPrice)
            .OrderByDescending(b => b.Amount)
            .ThenBy(b => b.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ownedQuantity = await _db.Pets
            .AsNoTracking()
            .Where(p => p.OwnerTraderId == traderId && p.BreedId == marketEntryId)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var eligibleAskQuantity = await _db.Pets
            .AsNoTracking()
            .Where(p =>
                p.OwnerTraderId == traderId &&
                p.BreedId == marketEntryId &&
                !_db.Listings.Any(l => l.PetId == p.Id && l.WithdrawnAt == null))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var traderSnapshot = await GetTraderSnapshotAsync(traderId, cancellationToken).ConfigureAwait(false);
        if (traderSnapshot is null)
        {
            return null;
        }

        var tradeEntities = await _db.Trades
            .AsNoTracking()
            .Include(t => t.Pet)
            .Include(t => t.Listing)
            .Where(t => t.Pet != null && t.Pet.BreedId == marketEntryId)
            .OrderByDescending(t => t.ExecutedAt)
            .Take(50)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var recentTrades = tradeEntities
            .Select(t => new TerminalTradeRow(
                t.Id,
                marketEntryId,
                t.Price,
                1,
                t.ExecutedAt,
                ClassifyTerminalExecutionType(t)))
            .ToList();

        var capturedAt = DateTimeOffset.UtcNow;
        var orderBook = new TerminalOrderBookSnapshot(
            marketEntryId,
            activeBids
                .GroupBy(b => b.Amount)
                .OrderByDescending(group => group.Key)
                .Select(group => new TerminalOrderBookLevel(
                    group.Key,
                    group.Count(),
                    group.Count(),
                    group.Min(b => b.CreatedAt)))
                .ToList(),
            activeListings
                .GroupBy(l => l.AskingPrice)
                .OrderBy(group => group.Key)
                .Select(group => new TerminalOrderBookLevel(
                    group.Key,
                    group.Count(),
                    group.Count(),
                    group.Min(l => l.CreatedAt)))
                .ToList(),
            capturedAt);

        var accountSummary = new TerminalAccountSummary(
            traderSnapshot.TraderId,
            traderSnapshot.DisplayName,
            traderSnapshot.AvailableCash,
            traderSnapshot.LockedCash,
            traderSnapshot.PortfolioTotal,
            ownedQuantity,
            eligibleAskQuantity);

        return new TerminalWorkspaceSnapshot(
            marketEntry,
            orderBook,
            accountSummary,
            recentTrades,
            capturedAt);
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

    public async Task<TerminalOrderResult?> PlaceTerminalBidAsync(
        Guid traderId,
        Guid marketEntryId,
        int quantity,
        decimal limitPrice,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0 || limitPrice <= 0)
        {
            return null;
        }

        if (!await _db.Breeds.AnyAsync(b => b.Id == marketEntryId, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var requestId = Guid.NewGuid();
        var tradeIds = new List<Guid>();
        var affectedTraderIds = new HashSet<Guid> { traderId };
        var filled = 0;
        decimal executionSum = 0;

        while (filled < quantity)
        {
            var cross = await TryExecuteSingleCrossBuyAsync(traderId, marketEntryId, limitPrice, cancellationToken)
                .ConfigureAwait(false);
            if (!cross.Success)
            {
                break;
            }

            filled++;
            executionSum += cross.Trade!.Price;
            tradeIds.Add(cross.Trade.TradeId);
            affectedTraderIds.Add(cross.Trade.BuyerTraderId);
            affectedTraderIds.Add(cross.Trade.SellerTraderId);
        }

        var remaining = quantity - filled;
        var pending = 0;
        if (remaining > 0)
        {
            var skip = new HashSet<Guid>();
            while (pending < remaining)
            {
                var listingId = await FindNextPendingBidListingIdAsync(
                        traderId,
                        marketEntryId,
                        limitPrice,
                        skip,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (listingId is null)
                {
                    break;
                }

                var placed = await TryExecuteSinglePendingBidAsync(traderId, listingId.Value, limitPrice, cancellationToken)
                    .ConfigureAwait(false);
                if (!placed.Success)
                {
                    skip.Add(listingId.Value);
                    continue;
                }

                pending++;
                affectedTraderIds.Add(placed.SellerTraderId);
                foreach (var priorBidderId in placed.ReleasedBidderIds)
                {
                    affectedTraderIds.Add(priorBidderId);
                }
            }
        }

        var rejected = quantity - filled - pending;
        var avg = filled > 0 ? executionSum / filled : (decimal?)null;
        var message = BuildTerminalBidMessage(filled, pending, rejected);
        return new TerminalOrderResult(
            requestId,
            TerminalOrderAction.PlaceBid,
            quantity,
            filled,
            pending,
            rejected,
            avg,
            message,
            tradeIds,
            affectedTraderIds.ToArray());
    }

    public async Task<TerminalOrderResult?> PlaceTerminalAskAsync(
        Guid traderId,
        Guid marketEntryId,
        int quantity,
        decimal limitPrice,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0 || limitPrice <= 0)
        {
            return null;
        }

        if (!await _db.Breeds.AnyAsync(b => b.Id == marketEntryId, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var requestId = Guid.NewGuid();

        var activelyListedPetIds = await _db.Listings
            .AsNoTracking()
            .Where(l => l.WithdrawnAt == null)
            .Select(l => l.PetId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var listedSet = new HashSet<Guid>(activelyListedPetIds);

        var breedPets = await _db.Pets
            .Where(p => p.OwnerTraderId == traderId && p.BreedId == marketEntryId)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pets = breedPets
            .Where(p => !listedSet.Contains(p.Id))
            .Take(quantity)
            .ToList();

        if (pets.Count < quantity)
        {
            return new TerminalOrderResult(
                requestId,
                TerminalOrderAction.PlaceAsk,
                quantity,
                0,
                0,
                quantity,
                null,
                "Insufficient eligible pets for the selected market.",
                [],
                [traderId]);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        foreach (var pet in pets)
        {
            var listing = new Listing
            {
                Id = Guid.NewGuid(),
                PetId = pet.Id,
                SellerTraderId = traderId,
                AskingPrice = limitPrice,
                CreatedAt = now,
                WithdrawnAt = null
            };
            _db.Listings.Add(listing);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new TerminalOrderResult(
            requestId,
            TerminalOrderAction.PlaceAsk,
            quantity,
            quantity,
            0,
            0,
            null,
            $"Listed {quantity} pet(s) at {limitPrice:0.##}.",
            [],
            [traderId]);
    }

    public async Task<TerminalOrderResult?> BuyNowAsync(
        Guid traderId,
        Guid marketEntryId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return null;
        }

        if (!await _db.Breeds.AnyAsync(b => b.Id == marketEntryId, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var requestId = Guid.NewGuid();

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var listings = await _db.Listings
            .Include(l => l.Pet!)
            .ThenInclude(p => p!.Breed)
            .Include(l => l.Seller)
            .Where(l =>
                l.WithdrawnAt == null &&
                l.Pet != null &&
                l.Pet.BreedId == marketEntryId &&
                l.SellerTraderId != traderId)
            .OrderBy(l => l.AskingPrice)
            .ThenBy(l => l.CreatedAt)
            .Take(quantity)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (listings.Count < quantity)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new TerminalOrderResult(
                requestId,
                TerminalOrderAction.BuyNow,
                quantity,
                0,
                0,
                quantity,
                null,
                "Requested quantity is not available at the best asks for this market.",
                [],
                [traderId]);
        }

        var buyer = await _db.Traders.FirstOrDefaultAsync(t => t.Id == traderId, cancellationToken).ConfigureAwait(false);
        if (buyer is null)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        var totalCost = listings.Sum(l => l.AskingPrice);
        if (buyer.AvailableCash < totalCost)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new TerminalOrderResult(
                requestId,
                TerminalOrderAction.BuyNow,
                quantity,
                0,
                0,
                quantity,
                null,
                "Insufficient available balance to complete the buy-now request.",
                [],
                [traderId]);
        }

        var tradeIds = new List<Guid>();
        var affectedTraderIds = new HashSet<Guid> { traderId };
        decimal executionSum = 0;
        var superseded = new List<Trader>();
        foreach (var listing in listings)
        {
            if (listing.Pet is null || listing.Seller is null)
            {
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return new TerminalOrderResult(
                    requestId,
                    TerminalOrderAction.BuyNow,
                    quantity,
                    0,
                    0,
                    quantity,
                    null,
                    "Requested quantity is not available at the best asks for this market.",
                    [],
                    [traderId]);
            }

            var more = await SupersedeActivePendingBidsWithNotificationsAsync(listing, cancellationToken)
                .ConfigureAwait(false);
            superseded.AddRange(more);

            var trade = await ExecuteTradeAsync(listing, buyer, listing.Seller, listing.AskingPrice, cancellationToken)
                .ConfigureAwait(false);
            executionSum += trade.Price;
            tradeIds.Add(trade.TradeId);
            affectedTraderIds.Add(trade.BuyerTraderId);
            affectedTraderIds.Add(trade.SellerTraderId);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        await PublishFundsLinkedAsync(buyer, cancellationToken).ConfigureAwait(false);
        foreach (var listing in listings)
        {
            if (listing.Seller is not null)
            {
                await PublishFundsLinkedAsync(listing.Seller, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (var prior in superseded.DistinctBy(t => t.Id))
        {
            affectedTraderIds.Add(prior.Id);
            await PublishFundsLinkedAsync(prior, cancellationToken).ConfigureAwait(false);
        }

        var avg = executionSum / quantity;
        return new TerminalOrderResult(
            requestId,
            TerminalOrderAction.BuyNow,
            quantity,
            quantity,
            0,
            0,
            avg,
            $"Bought {quantity} pet(s) at the best available asks (average {avg:0.##}).",
            tradeIds,
            affectedTraderIds.ToArray());
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

    private async Task<Dictionary<Guid, TerminalMarketProjection>> BuildTerminalMarketDataAsync(
        CancellationToken cancellationToken)
    {
        var activeListings = await _db.Listings
            .AsNoTracking()
            .Include(l => l.Pet)
            .Where(l => l.WithdrawnAt == null && l.Pet != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var activeBids = await _db.Bids
            .AsNoTracking()
            .Include(b => b.Listing!)
            .ThenInclude(l => l!.Pet)
            .Where(b =>
                b.Status == BidStatus.Active &&
                b.Listing != null &&
                b.Listing.WithdrawnAt == null &&
                b.Listing.Pet != null &&
                b.Amount < b.Listing.AskingPrice)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var trades = await _db.Trades
            .AsNoTracking()
            .Include(t => t.Pet)
            .Where(t => t.Pet != null)
            .OrderByDescending(t => t.ExecutedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var bestAskByBreed = activeListings
            .GroupBy(l => l.Pet!.BreedId)
            .ToDictionary(group => group.Key, group => (decimal?)group.Min(l => l.AskingPrice));

        var bestBidByBreed = activeBids
            .GroupBy(b => b.Listing!.Pet!.BreedId)
            .ToDictionary(group => group.Key, group => (decimal?)group.Max(b => b.Amount));

        var result = new Dictionary<Guid, TerminalMarketProjection>();
        foreach (var tradeGroup in trades.GroupBy(t => t.Pet!.BreedId))
        {
            var orderedTrades = tradeGroup.OrderByDescending(t => t.ExecutedAt).ToList();
            var latest = orderedTrades[0];
            var previous = orderedTrades.Count > 1 ? orderedTrades[1] : null;
            var trendDirection = previous is null
                ? TerminalTrendDirection.Flat
                : latest.Price > previous.Price
                    ? TerminalTrendDirection.Up
                    : latest.Price < previous.Price
                        ? TerminalTrendDirection.Down
                        : TerminalTrendDirection.Flat;

            result[tradeGroup.Key] = new TerminalMarketProjection(
                latest.Price,
                bestBidByBreed.GetValueOrDefault(tradeGroup.Key),
                bestAskByBreed.GetValueOrDefault(tradeGroup.Key),
                trendDirection,
                latest.ExecutedAt);
        }

        foreach (var pair in bestAskByBreed)
        {
            if (!result.ContainsKey(pair.Key))
            {
                result[pair.Key] = new TerminalMarketProjection(
                    null,
                    bestBidByBreed.GetValueOrDefault(pair.Key),
                    pair.Value,
                    TerminalTrendDirection.NoTradeData,
                    null);
            }
        }

        foreach (var pair in bestBidByBreed)
        {
            if (!result.ContainsKey(pair.Key))
            {
                result[pair.Key] = new TerminalMarketProjection(
                    null,
                    pair.Value,
                    bestAskByBreed.GetValueOrDefault(pair.Key),
                    TerminalTrendDirection.NoTradeData,
                    null);
            }
        }

        return result;
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

    private async Task<TerminalCrossBuyAttempt> TryExecuteSingleCrossBuyAsync(
        Guid buyerTraderId,
        Guid marketEntryId,
        decimal limitPrice,
        CancellationToken cancellationToken)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var listing = await _db.Listings
            .Include(l => l.Pet!)
            .ThenInclude(p => p!.Breed)
            .Include(l => l.Seller)
            .Where(l =>
                l.WithdrawnAt == null &&
                l.Pet != null &&
                l.Pet.BreedId == marketEntryId &&
                l.SellerTraderId != buyerTraderId &&
                l.AskingPrice <= limitPrice)
            .OrderBy(l => l.AskingPrice)
            .ThenBy(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (listing is null || listing.Pet is null || listing.Seller is null)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new TerminalCrossBuyAttempt(false, null);
        }

        var buyer = await _db.Traders.FirstOrDefaultAsync(t => t.Id == buyerTraderId, cancellationToken)
            .ConfigureAwait(false);
        if (buyer is null)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new TerminalCrossBuyAttempt(false, null);
        }

        if (buyer.AvailableCash < listing.AskingPrice)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return new TerminalCrossBuyAttempt(false, null);
        }

        var superseded = await SupersedeActivePendingBidsWithNotificationsAsync(listing, cancellationToken)
            .ConfigureAwait(false);
        var trade = await ExecuteTradeAsync(listing, buyer, listing.Seller, listing.AskingPrice, cancellationToken)
            .ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);

        await PublishFundsLinkedAsync(buyer, cancellationToken).ConfigureAwait(false);
        await PublishFundsLinkedAsync(listing.Seller, cancellationToken).ConfigureAwait(false);
        foreach (var prior in superseded)
        {
            await PublishFundsLinkedAsync(prior, cancellationToken).ConfigureAwait(false);
        }

        return new TerminalCrossBuyAttempt(true, trade);
    }

    private async Task<Guid?> FindNextPendingBidListingIdAsync(
        Guid buyerTraderId,
        Guid marketEntryId,
        decimal limitPrice,
        HashSet<Guid> skip,
        CancellationToken cancellationToken)
    {
        var listing = await _db.Listings
            .AsNoTracking()
            .Include(l => l.Pet)
            .Where(l =>
                l.WithdrawnAt == null &&
                l.Pet != null &&
                l.Pet.BreedId == marketEntryId &&
                l.SellerTraderId != buyerTraderId &&
                l.AskingPrice > limitPrice &&
                !skip.Contains(l.Id))
            .OrderBy(l => l.AskingPrice)
            .ThenBy(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return listing?.Id;
    }

    private async Task<TerminalPendingBidAttempt> TryExecuteSinglePendingBidAsync(
        Guid buyerTraderId,
        Guid listingId,
        decimal limitPrice,
        CancellationToken cancellationToken)
    {
        if (limitPrice <= 0)
        {
            return TerminalPendingBidAttempt.Failed;
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
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return TerminalPendingBidAttempt.Failed;
        }

        if (listing.SellerTraderId == buyerTraderId)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return TerminalPendingBidAttempt.Failed;
        }

        if (limitPrice >= listing.AskingPrice)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return TerminalPendingBidAttempt.Failed;
        }

        var buyer = await _db.Traders.FirstOrDefaultAsync(t => t.Id == buyerTraderId, cancellationToken)
            .ConfigureAwait(false);
        if (buyer is null)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return TerminalPendingBidAttempt.Failed;
        }

        if (!await HasSpendableCashAsync(buyer, limitPrice, cancellationToken).ConfigureAwait(false))
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return TerminalPendingBidAttempt.Failed;
        }

        var releasedBidderIds = new List<Guid>();
        var activeBid = await _db.Bids
            .FirstOrDefaultAsync(
                b => b.ListingId == listing.Id && b.Status == BidStatus.Active,
                cancellationToken)
            .ConfigureAwait(false);
        if (activeBid is not null && limitPrice <= activeBid.Amount)
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return TerminalPendingBidAttempt.Failed;
        }

        if (activeBid is not null)
        {
            await ReleaseBidLockAsync(activeBid, cancellationToken).ConfigureAwait(false);
            activeBid.Status = BidStatus.Superseded;
            await AddOutbidNotificationAsync(activeBid, listing, cancellationToken).ConfigureAwait(false);
            releasedBidderIds.Add(activeBid.BuyerTraderId);
        }

        if (!await TryDebitSpendableCashAsync(buyer, limitPrice, cancellationToken).ConfigureAwait(false))
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return TerminalPendingBidAttempt.Failed;
        }

        buyer.LockedCash += limitPrice;
        var bid = new Bid
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            BuyerTraderId = buyerTraderId,
            Amount = limitPrice,
            Status = BidStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Bids.Add(bid);
        await AddBidReceivedNotificationAsync(listing, bid, buyer, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        await PublishFundsLinkedAsync(buyer, cancellationToken).ConfigureAwait(false);
        return new TerminalPendingBidAttempt(true, listing.SellerTraderId, releasedBidderIds);
    }

    private static string BuildTerminalBidMessage(int filled, int pending, int rejected)
    {
        if (rejected == 0)
        {
            return $"Bid processed: {filled} filled at or below the limit, {pending} new pending below-ask bid(s).";
        }

        if (filled == 0 && pending == 0)
        {
            return "Bid could not be placed: no matching asks, no below-ask listings to bid on, or insufficient balance.";
        }

        return $"Bid partially processed: {filled} filled, {pending} new pending bid(s), {rejected} not placed against available listings.";
    }

    private sealed record TerminalCrossBuyAttempt(bool Success, TradeResultRow? Trade);
    private sealed record TerminalPendingBidAttempt(
        bool Success,
        Guid SellerTraderId,
        IReadOnlyList<Guid> ReleasedBidderIds)
    {
        public static TerminalPendingBidAttempt Failed { get; } = new(false, Guid.Empty, []);
    }

    private sealed record TerminalMarketProjection(
        decimal? LatestTradePrice,
        decimal? BestBidPrice,
        decimal? BestAskPrice,
        TerminalTrendDirection TrendDirection,
        DateTimeOffset? LastTradeAt);

    private static string ClassifyTerminalExecutionType(Trade t)
    {
        if (t.Listing is null)
        {
            return "SecondaryMarket";
        }

        return t.Price >= t.Listing.AskingPrice ? "BuyNow" : "BidAccept";
    }
}
