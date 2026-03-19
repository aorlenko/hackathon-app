using MarketService.Application.Orders;
using Trading.Contracts.Http;

namespace MarketService.Api.Endpoints;

public static class OrdersEndpoints
{
    public static RouteGroupBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").RequireAuthorization();
        group.MapPost(string.Empty, PlaceOrder);
        return group;
    }

    public static async Task<IResult> PlaceOrder(HttpContext context, PlaceOrderRequest request, PlaceOrderHandler handler, CancellationToken cancellationToken)
    {
        var user = CurrentUserProfileReader.Read(context.User);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var outcome = await handler.HandleAsync(user, request, cancellationToken).ConfigureAwait(false);
            return Results.Accepted($"/api/orders/{outcome.Response.OrderId}", outcome.Response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new ErrorResponse("order_rejected", ex.Message, null, Guid.NewGuid().ToString("N")));
        }
    }
}
