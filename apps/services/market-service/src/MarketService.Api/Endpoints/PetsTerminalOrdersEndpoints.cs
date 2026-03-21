using System.Security.Claims;
using MarketService.Application.Abstractions;
using MarketService.Application.Pets.Terminal;
using MarketService.Application.Realtime;

namespace MarketService.Api.Endpoints;

public static class PetsTerminalOrdersEndpoints
{
    public static RouteGroupBuilder MapPetsTerminalOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pets/terminal/orders").RequireAuthorization();
        group.MapPost("/bid", PlaceBidAsync);
        group.MapPost("/ask", PlaceAskAsync);
        group.MapPost("/buy-now", BuyNowAsync);
        return group;
    }

    private static async Task<IResult> PlaceBidAsync(
        HttpContext httpContext,
        PlaceTerminalOrderRequest body,
        PlaceTerminalOrderHandler handler,
        IMarketPetStore store,
        IMarketRealtimeNotifier realtime,
        CancellationToken cancellationToken)
    {
        return await HandleAsync(
                httpContext,
                body,
                store,
                realtime,
                (tid, req, ct) => handler.PlaceBidAsync(tid, req, ct),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> PlaceAskAsync(
        HttpContext httpContext,
        PlaceTerminalOrderRequest body,
        PlaceTerminalOrderHandler handler,
        IMarketPetStore store,
        IMarketRealtimeNotifier realtime,
        CancellationToken cancellationToken)
    {
        return await HandleAsync(
                httpContext,
                body,
                store,
                realtime,
                (tid, req, ct) => handler.PlaceAskAsync(tid, req, ct),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> BuyNowAsync(
        HttpContext httpContext,
        PlaceTerminalOrderRequest body,
        PlaceTerminalOrderHandler handler,
        IMarketPetStore store,
        IMarketRealtimeNotifier realtime,
        CancellationToken cancellationToken)
    {
        return await HandleAsync(
                httpContext,
                body,
                store,
                realtime,
                (tid, req, ct) => handler.BuyNowAsync(tid, req, ct),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        PlaceTerminalOrderRequest body,
        IMarketPetStore store,
        IMarketRealtimeNotifier realtime,
        Func<Guid, PlaceTerminalOrderRequest, CancellationToken, Task<TerminalOrderResult?>> execute,
        CancellationToken cancellationToken)
    {
        var sub = httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(sub))
        {
            return Results.Unauthorized();
        }

        var displayName = httpContext.User.FindFirst("name")?.Value
            ?? httpContext.User.FindFirst(ClaimTypes.Name)?.Value
            ?? httpContext.User.FindFirst("email")?.Value
            ?? sub;

        var email = httpContext.User.FindFirst("email")?.Value;

        var traderId = await store.EnsureLinkedTraderForUserAsync(sub, displayName, email, cancellationToken)
            .ConfigureAwait(false);

        var result = await execute(traderId, body, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return Results.BadRequest(new { message = "Invalid terminal order request." });
        }

        if (result.FilledQuantity == 0 && result.PendingQuantity == 0 && result.RejectedQuantity == result.RequestedQuantity)
        {
            return Results.BadRequest(new { message = result.Message });
        }

        await realtime.NotifyTradingPetsStateChangedAsync(
                result.AffectedTraderIds,
                includeNotifications: true,
                includeLeaderboard: true,
                cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(ToOrderResultDto(result));
    }

    private static object ToOrderResultDto(TerminalOrderResult r) =>
        new
        {
            requestId = r.RequestId,
            action = r.Action.ToString(),
            requestedQuantity = r.RequestedQuantity,
            filledQuantity = r.FilledQuantity,
            pendingQuantity = r.PendingQuantity,
            rejectedQuantity = r.RejectedQuantity,
            averageExecutedPrice = r.AverageExecutedPrice,
            message = r.Message,
            affectedTradeIds = r.AffectedTradeIds
        };
}
