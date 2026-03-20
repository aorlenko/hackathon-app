using MarketService.Application.Abstractions;
using Trading.Contracts.Http;

namespace MarketService.Application.Accounts;

public sealed class GetCurrentAccountHandler
{
    private readonly IMarketDataStore _store;

    public GetCurrentAccountHandler(IMarketDataStore store)
    {
        _store = store;
    }

    public async Task<AccountSnapshotDto?> HandleAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Authenticated user is required.");
        }

        var account = await _store.GetAccountAsync(userId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return null;
        }

        return new AccountSnapshotDto(
            account.UserId,
            account.DisplayName,
            account.Email,
            account.CashAvailable);
    }
}
