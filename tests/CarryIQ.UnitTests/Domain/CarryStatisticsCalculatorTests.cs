namespace CarryIQ.UnitTests.Domain;

public class CarryStatisticsCalculatorTests
{
    [Fact]
    public void CalculateReturnsExpectedSummaryValues()
    {
        var statistics = CarryStatisticsCalculator.Calculate(
            [
                Distance.FromYards(90m),
                Distance.FromYards(100m),
                Distance.FromYards(110m),
            ]);

        Assert.Equal(3, statistics.SampleCount);
        Assert.Equal(100m, statistics.MeanCarry.Yards);
        Assert.Equal(100m, statistics.MedianCarry.Yards);
        Assert.Equal(90m, statistics.MinimumCarry.Yards);
        Assert.Equal(110m, statistics.MaximumCarry.Yards);
        Assert.Equal(20m, statistics.CarryRange.Yards);
        Assert.Equal(10m, statistics.CarryStandardDeviation.Yards);
        Assert.Equal(0.1m, statistics.CoefficientOfVariation);
        Assert.Equal(20m, statistics.InterquartileRange.Yards);
    }

    [Fact]
    public void ConsistencyScoreUsesNormalisedVariability()
    {
        var statistics = CarryStatisticsCalculator.Calculate(
            [
                Distance.FromYards(90m),
                Distance.FromYards(100m),
                Distance.FromYards(110m),
            ]);

        Assert.Equal(90m, ConsistencyScoreCalculator.Calculate(statistics));
    }
}
