namespace Trading.Contracts.Http;

public sealed record BootstrapDemoAccountRequest(string? DisplayName, string? Email);

public sealed record DemoHoldingDto(string Symbol, int Quantity);

public sealed record DemoAccountDto(
    string UserId,
    string DisplayName,
    string Email,
    decimal CashAvailable,
    IReadOnlyList<DemoHoldingDto> Holdings);
