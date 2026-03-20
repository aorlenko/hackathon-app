using MarketService.Application.Pets;

namespace MarketService.UnitTests;

public sealed class PetValuationTests
{
    [Fact]
    public void Intrinsic_value_matches_hand_calculation_for_labrador_defaults()
    {
        var value = PetIntrinsicValueCalculator.Calculate(
            retailPrice: 100m,
            health: 100m,
            desirability: 8,
            ageYears: 0m,
            lifespanYears: 12m);

        Assert.Equal(80.00m, value);
    }

    [Fact]
    public void Intrinsic_value_is_zero_when_age_reaches_lifespan()
    {
        var value = PetIntrinsicValueCalculator.Calculate(
            retailPrice: 100m,
            health: 100m,
            desirability: 8,
            ageYears: 12m,
            lifespanYears: 12m);

        Assert.Equal(0m, value);
    }

    [Fact]
    public void Intrinsic_value_clamps_health_and_desirability()
    {
        var highHealth = PetIntrinsicValueCalculator.Calculate(10m, 150m, 15, 0m, 10m);
        var lowDesirability = PetIntrinsicValueCalculator.Calculate(10m, 100m, 0, 0m, 10m);
        Assert.True(highHealth > 0m);
        Assert.True(lowDesirability > 0m);
    }
}
