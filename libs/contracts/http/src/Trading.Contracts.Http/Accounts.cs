namespace Trading.Contracts.Http;

public sealed record BootstrapDemoAccountRequest(string? DisplayName, string? Email);

public sealed record ResolveAccountsRequest(IReadOnlyList<string> UserIds);

public sealed record AccountIdentityDto(
    string UserId,
    string DisplayName,
    string Email);

public sealed record DemoHoldingDto(string Symbol, int Quantity);

public sealed record DemoAccountDto(
    string UserId,
    string DisplayName,
    string Email,
    decimal CashAvailable,
    IReadOnlyList<DemoHoldingDto> Holdings);

public sealed record AccountSnapshotDto(
    string UserId,
    string DisplayName,
    string Email,
    decimal CashAvailable,
    IReadOnlyList<DemoHoldingDto> Holdings);
