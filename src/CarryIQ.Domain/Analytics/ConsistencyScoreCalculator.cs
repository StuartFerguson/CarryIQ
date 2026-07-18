namespace CarryIQ.Domain;

public static class ConsistencyScoreCalculator
{
    public static decimal Calculate(CarryStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        if (statistics.SampleCount == 0 || statistics.MeanCarry.Yards <= 0m)
        {
            return 0m;
        }

        var normalisedVariability = (statistics.CarryStandardDeviation.Yards / statistics.MeanCarry.Yards) * 100m;
        return Math.Clamp(100m - normalisedVariability, 0m, 100m);
    }
}
