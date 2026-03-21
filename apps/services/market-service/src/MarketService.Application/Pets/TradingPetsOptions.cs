namespace MarketService.Application.Pets;

public sealed class TradingPetsOptions
{
    public const string SectionName = "TradingPets";

    /// <summary>Default pet age-years per valuation tick (matches former <c>ValuationTickSeconds / (365.25 * 24 * 3600)</c> when <see cref="ValuationTickSeconds"/> is 60).</summary>
    public static decimal DefaultAgeYearsEveryMinute => 60m / (365.25m * 24m * 3600m);

    public decimal InitialTraderCash { get; set; } = 1000m;
    public int DefaultSupplyPerBreed { get; set; } = 3;
    public int ValuationTickSeconds { get; set; } = 60;
    /// <summary>Pet age-years added on each valuation tick (<c>1</c> = one year, <c>0.5</c> = six months). <see cref="ValuationTickSeconds"/> only controls how often the tick runs.</summary>
    public decimal AgeYearsEveryMinute { get; set; } = DefaultAgeYearsEveryMinute;
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
