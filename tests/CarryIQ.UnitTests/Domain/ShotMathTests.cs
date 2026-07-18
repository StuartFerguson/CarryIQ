namespace CarryIQ.UnitTests.Domain;

public class ShotMathTests
{
    [Fact]
    public void CalculateSmashFactorReturnsRatioWhenBothSpeedsArePresent()
    {
        var smashFactor = ShotMath.CalculateSmashFactor(
            Speed.FromMilesPerHour(150m),
            Speed.FromMilesPerHour(100m));

        Assert.Equal(1.5m, smashFactor);
    }

    [Fact]
    public void CalculateSmashFactorReturnsNullForMissingOrInvalidInputs()
    {
        Assert.Null(ShotMath.CalculateSmashFactor(null, Speed.FromMilesPerHour(100m)));
        Assert.Null(ShotMath.CalculateSmashFactor(Speed.FromMilesPerHour(150m), null));
        Assert.Null(ShotMath.CalculateSmashFactor(Speed.FromMilesPerHour(150m), Speed.Zero));
    }
}
