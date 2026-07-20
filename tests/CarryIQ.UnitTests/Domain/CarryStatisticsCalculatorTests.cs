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

    [Fact]
    public void ClubAnalyticsCalculatorOrdersClubsAndCalculatesGaps()
    {
        var result = ClubAnalyticsCalculator.Calculate(
            [
                ("7 Iron", new[]
                {
                    Distance.FromYards(150m),
                    Distance.FromYards(152m),
                    Distance.FromYards(148m),
                }),
                ("8 Iron", new[]
                {
                    Distance.FromYards(140m),
                    Distance.FromYards(138m),
                    Distance.FromYards(142m),
                }),
            ],
            ClubGapOption.Median);

        Assert.Equal(2, result.Clubs.Count);
        Assert.Equal("8 Iron", result.Clubs[0].ClubName);
        Assert.Equal("7 Iron", result.Clubs[1].ClubName);
        Assert.Equal(10m, result.Gaps[0].GapYards);
        Assert.False(result.Gaps[0].HasOverlap);
        Assert.False(result.Gaps[0].HasWarning);
    }

    [Fact]
    public void ClubAnalyticsCalculatorFlagsInsufficientSampleCounts()
    {
        var result = ClubAnalyticsCalculator.Calculate(
            [
                ("Driver", new[]
                {
                    Distance.FromYards(260m),
                    Distance.FromYards(255m),
                }),
            ],
            ClubGapOption.Mean,
            sampleWarningThreshold: 3);

        Assert.Single(result.Clubs);
        Assert.True(result.Clubs[0].HasInsufficientSamples);
    }
}
