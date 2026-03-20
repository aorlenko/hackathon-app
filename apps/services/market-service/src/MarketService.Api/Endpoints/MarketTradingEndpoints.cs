using MarketService.Application.Abstractions;
using MarketService.Application.Authorization;
using MarketService.Application.Market;
using MarketService.Application.Realtime;

namespace MarketService.Api.Endpoints;

public static class MarketTradingEndpoints
{
    public static RouteGroupBuilder MapMarketTradingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/market").RequireAuthorization();
        group.MapGet("/listings", GetListingsAsync);
        group.MapPost("/listings", CreateListingAsync);
        group.MapPost("/listings/{listingId:guid}/withdraw", WithdrawListingAsync);
        group.MapPost("/listings/{listingId:guid}/bids", PlaceBidAsync);
        group.MapPost("/listings/{listingId:guid}/accept", AcceptBidAsync);
        group.MapPost("/listings/{listingId:guid}/reject", RejectBidAsync);
        group.MapPost("/bids/{bidId:guid}/withdraw", WithdrawBidAsync);
        return group;
    }

    private static async Task<IResult> GetListingsAsync(
        IMarketPetStore store,
        CancellationToken cancellationToken)
    {
        var rows = await store.GetMarketListingsAsync(cancellationToken).ConfigureAwait(false);
        var payload = rows.Select(l => new
        {
            listingId = l.ListingId,
            petId = l.PetId,
            sellerTraderId = l.SellerTraderId,
            breedName = l.BreedName,
            askingPrice = l.AskingPrice,
            sellerDisplayName = l.SellerDisplayName,
            createdAt = l.CreatedAt,
            recentTradePriceForBreed = l.RecentTradePriceForBreed,
            remainingNewSupplyForBreed = l.RemainingNewSupplyForBreed
        });
        return Results.Ok(payload);
    }

    private static async Task<IResult> CreateListingAsync(
        HttpContext httpContext,
        CreateListingBody body,
        SecondaryMarketHandlers handlers,
        TraderAuthorizationHelper authorization,
        ITradingPetsRealtimePublisher realtime,
        CancellationToken cancellationToken)
    {
        var guard = await TradingPetsAuth.RequireTraderAsync(httpContext, body.TraderId, authorization, cancellationToken)
            .ConfigureAwait(false);
        if (guard is not null)
        {
            return guard;
        }

        if (!Guid.TryParse(body.PetId, out var petId))
        {
            return Results.BadRequest(new
            {
                error = "invalid_pet_id",
                message = "PetId must be a valid UUID. Select a pet from your inventory."
            });
        }

        var id = await handlers
            .CreateListingAsync(body.TraderId, petId, body.AskingPrice, cancellationToken)
            .ConfigureAwait(false);
        if (id is null)
        {
            return Results.BadRequest(new
            {
                error = "listing_rejected",
                message = "Cannot create listing (check pet ownership, asking price, or an active listing may already exist)."
            });
        }

        await realtime.NotifyMarketListingsRefreshAsync(cancellationToken).ConfigureAwait(false);
        await realtime.NotifyTraderSnapshotRefreshAsync(body.TraderId, cancellationToken).ConfigureAwait(false);
        return Results.Created($"/api/market/listings/{id}", new { listingId = id });
    }

    private static async Task<IResult> WithdrawListingAsync(
        HttpContext httpContext,
        Guid listingId,
        TraderIdBody body,
        SecondaryMarketHandlers handlers,
        TraderAuthorizationHelper authorization,
        ITradingPetsRealtimePublisher realtime,
        CancellationToken cancellationToken)
    {
        var guard = await TradingPetsAuth.RequireTraderAsync(httpContext, body.TraderId, authorization, cancellationToken)
            .ConfigureAwait(false);
        if (guard is not null)
        {
            return guard;
        }

        var ok = await handlers.WithdrawListingAsync(body.TraderId, listingId, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return Results.BadRequest(new { error = "withdraw_failed", message = "Listing cannot be withdrawn." });
        }

        await realtime.NotifyMarketListingsRefreshAsync(cancellationToken).ConfigureAwait(false);
        await realtime.NotifyTraderSnapshotRefreshAsync(body.TraderId, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> PlaceBidAsync(
        HttpContext httpContext,
        Guid listingId,
        PlaceBidBody body,
        SecondaryMarketHandlers handlers,
        TraderAuthorizationHelper authorization,
        ITradingPetsRealtimePublisher realtime,
        IMarketPetStore store,
        CancellationToken cancellationToken)
    {
        var guard = await TradingPetsAuth.RequireTraderAsync(httpContext, body.TraderId, authorization, cancellationToken)
            .ConfigureAwait(false);
        if (guard is not null)
        {
            return guard;
        }

        var result = await handlers
            .PlaceBidAsync(body.TraderId, listingId, body.Amount, cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
        {
            return Results.BadRequest(new
            {
                error = "bid_rejected",
                message = "Bid rejected (self-bid, insufficient cash, inactive listing, or below-ask amount not higher than current bid)."
            });
        }

        if (result is CrossTradePlaceResult cross)
        {
            await PushTradeSideEffectsAsync(realtime, store, cross.Trade, cancellationToken).ConfigureAwait(false);
            return Results.Ok(new
            {
                executed = true,
                trade = new
                {
                    tradeId = cross.Trade.TradeId,
                    petId = cross.Trade.PetId,
                    buyerTraderId = cross.Trade.BuyerTraderId,
                    sellerTraderId = cross.Trade.SellerTraderId,
                    price = cross.Trade.Price
                }
            });
        }

        if (result is ActiveBidPlaceResult pending)
        {
            await realtime.NotifyTraderSnapshotRefreshAsync(body.TraderId, cancellationToken).ConfigureAwait(false);
            var listingRow = (await store.GetMarketListingsAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(l => l.ListingId == listingId);
            if (listingRow is not null)
            {
                await realtime.NotifyTraderSnapshotRefreshAsync(listingRow.SellerTraderId, cancellationToken)
                    .ConfigureAwait(false);
                await realtime.NotifyTraderNotificationsAsync(listingRow.SellerTraderId, cancellationToken)
                    .ConfigureAwait(false);
            }

            await realtime.NotifyTraderNotificationsAsync(body.TraderId, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/market/bids/{pending.BidId}", new { bidId = pending.BidId, status = "Active" });
        }

        return Results.BadRequest(new { error = "bid_rejected", message = "Unexpected bid outcome." });
    }

    private static async Task<IResult> WithdrawBidAsync(
        HttpContext httpContext,
        Guid bidId,
        TraderIdBody body,
        SecondaryMarketHandlers handlers,
        TraderAuthorizationHelper authorization,
        ITradingPetsRealtimePublisher realtime,
        IMarketPetStore store,
        CancellationToken cancellationToken)
    {
        var guard = await TradingPetsAuth.RequireTraderAsync(httpContext, body.TraderId, authorization, cancellationToken)
            .ConfigureAwait(false);
        if (guard is not null)
        {
            return guard;
        }

        var ok = await handlers.WithdrawBidAsync(body.TraderId, bidId, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return Results.BadRequest(new { error = "withdraw_failed", message = "Bid cannot be withdrawn." });
        }

        await realtime.NotifyTraderSnapshotRefreshAsync(body.TraderId, cancellationToken).ConfigureAwait(false);
        await realtime.NotifyMarketListingsRefreshAsync(cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> AcceptBidAsync(
        HttpContext httpContext,
        Guid listingId,
        TraderIdBody body,
        SecondaryMarketHandlers handlers,
        TraderAuthorizationHelper authorization,
        ITradingPetsRealtimePublisher realtime,
        IMarketPetStore store,
        CancellationToken cancellationToken)
    {
        var guard = await TradingPetsAuth.RequireTraderAsync(httpContext, body.TraderId, authorization, cancellationToken)
            .ConfigureAwait(false);
        if (guard is not null)
        {
            return guard;
        }

        var trade = await handlers.AcceptBidAsync(body.TraderId, listingId, cancellationToken).ConfigureAwait(false);
        if (trade is null)
        {
            return Results.Conflict(new { error = "no_pending_bid", message = "No active below-ask bid to accept." });
        }

        await PushTradeSideEffectsAsync(realtime, store, trade, cancellationToken).ConfigureAwait(false);
        return Results.Ok(new
        {
            tradeId = trade.TradeId,
            petId = trade.PetId,
            buyerTraderId = trade.BuyerTraderId,
            sellerTraderId = trade.SellerTraderId,
            price = trade.Price
        });
    }

    private static async Task<IResult> RejectBidAsync(
        HttpContext httpContext,
        Guid listingId,
        TraderIdBody body,
        SecondaryMarketHandlers handlers,
        TraderAuthorizationHelper authorization,
        ITradingPetsRealtimePublisher realtime,
        CancellationToken cancellationToken)
    {
        var guard = await TradingPetsAuth.RequireTraderAsync(httpContext, body.TraderId, authorization, cancellationToken)
            .ConfigureAwait(false);
        if (guard is not null)
        {
            return guard;
        }

        var ok = await handlers.RejectBidAsync(body.TraderId, listingId, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            return Results.Conflict(new { error = "no_pending_bid", message = "No active below-ask bid to reject." });
        }

        await realtime.NotifyMarketListingsRefreshAsync(cancellationToken).ConfigureAwait(false);
        await realtime.NotifyTraderSnapshotRefreshAsync(body.TraderId, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task PushTradeSideEffectsAsync(
        ITradingPetsRealtimePublisher realtime,
        IMarketPetStore store,
        TradeResultRow trade,
        CancellationToken cancellationToken)
    {
        await realtime.NotifyTraderSnapshotRefreshAsync(trade.BuyerTraderId, cancellationToken).ConfigureAwait(false);
        await realtime.NotifyTraderSnapshotRefreshAsync(trade.SellerTraderId, cancellationToken).ConfigureAwait(false);
        await realtime.NotifyTraderNotificationsAsync(trade.BuyerTraderId, cancellationToken).ConfigureAwait(false);
        await realtime.NotifyTraderNotificationsAsync(trade.SellerTraderId, cancellationToken).ConfigureAwait(false);
        await realtime.NotifyMarketListingsRefreshAsync(cancellationToken).ConfigureAwait(false);
        await realtime.NotifyLeaderboardRefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    public sealed record CreateListingBody(Guid TraderId, string PetId, decimal AskingPrice);

    public sealed record TraderIdBody(Guid TraderId);

    public sealed record PlaceBidBody(Guid TraderId, decimal Amount);
}
