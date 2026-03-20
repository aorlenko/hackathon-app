namespace MarketService.Application.Pets;

public static class PetIntrinsicValueCalculator
{
    public static decimal Calculate(
        decimal retailPrice,
        decimal health,
        int desirability,
        decimal ageYears,
        decimal lifespanYears)
    {
        if (lifespanYears <= 0)
        {
            return 0m;
        }

        var h = Math.Clamp(health, 0m, 100m);
        var d = Math.Clamp(desirability, 1, 10);
        var ageFactor = Math.Max(0m, 1m - ageYears / lifespanYears);
        var raw = retailPrice * (h / 100m) * (d / 10m) * ageFactor;
        return Math.Round(raw, 2, MidpointRounding.AwayFromZero);
    }
}
