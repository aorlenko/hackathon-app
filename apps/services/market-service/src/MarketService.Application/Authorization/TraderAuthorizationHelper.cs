using System.Security.Claims;
using MarketService.Application.Abstractions;

namespace MarketService.Application.Authorization;

public sealed class TraderAuthorizationHelper
{
    private readonly IMarketPetStore _store;

    public TraderAuthorizationHelper(IMarketPetStore store)
    {
        _store = store;
    }

    public async Task<bool> CanActAsTraderAsync(
        ClaimsPrincipal user,
        Guid traderId,
        CancellationToken cancellationToken = default)
    {
        var sub = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return await _store
            .IsAuthorizedTraderCommandAsync(traderId, sub, cancellationToken)
            .ConfigureAwait(false);
    }
}
