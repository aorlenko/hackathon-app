using System.Text.Json.Serialization;

namespace Trading.Contracts.Http;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderSideDto
{
    BUY,
    SELL
}

public sealed record PlaceOrderRequest(string ItemSymbol, OrderSideDto Side, decimal Price, int Quantity);

public sealed record PlaceOrderResponse(Guid OrderId, string Status, DateTimeOffset AcceptedAtUtc, int RemainingQuantity);

public sealed record ErrorResponse(string Code, string Message, object? Details, string CorrelationId);
