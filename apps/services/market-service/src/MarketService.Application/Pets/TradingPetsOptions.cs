namespace MarketService.Application.Pets;

public sealed class TradingPetsOptions
{
    public const string SectionName = "TradingPets";

    public decimal InitialTraderCash { get; set; } = 1000m;
    public int DefaultSupplyPerBreed { get; set; } = 3;
    public int ValuationTickSeconds { get; set; } = 60;
    /// <summary>Demo-only seeded traders (fixed GUIDs). Use 0 in production so each user gets a linked trader via <c>/api/traders/me/snapshot</c>.</summary>
    public int TraderSeedCount { get; set; }
    /// <summary>When false, commands are allowed only for traders whose <c>ExternalUserId</c> matches the JWT subject (or when <see cref="AllowAnyAuthenticatedUserForAllTraders"/> is true for tests).</summary>
    public bool AllowAnyAuthenticatedUserForAllTraders { get; set; }

    /// <summary>Seeded traders + permissive auth for in-memory contract tests.</summary>
    public static TradingPetsOptions CreateForContractTestHarness() =>
        new()
        {
            TraderSeedCount = 3,
            AllowAnyAuthenticatedUserForAllTraders = true,
        };
}
