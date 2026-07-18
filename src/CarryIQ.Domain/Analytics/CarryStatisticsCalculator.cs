namespace CarryIQ.Domain;

public static class CarryStatisticsCalculator
{
    public static CarryStatistics Calculate(IEnumerable<Distance> carries)
    {
        ArgumentNullException.ThrowIfNull(carries);

        var samples = carries.Select(carry => carry.Yards).OrderBy(value => value).ToArray();
        if (samples.Length == 0)
        {
            return new CarryStatistics
            {
                MeanCarry = Distance.Zero,
                MedianCarry = Distance.Zero,
                MinimumCarry = Distance.Zero,
                MaximumCarry = Distance.Zero,
                CarryRange = Distance.Zero,
                CarryStandardDeviation = Distance.Zero,
                CoefficientOfVariation = 0m,
                InterquartileRange = Distance.Zero,
                SampleCount = 0,
            };
        }

        var mean = samples.Average();
        var median = Median(samples);
        var minimum = samples[0];
        var maximum = samples[^1];
        var variance = samples.Length > 1
            ? samples.Sum(sample => (sample - mean) * (sample - mean)) / (samples.Length - 1)
            : 0m;
        var standardDeviation = (decimal)Math.Sqrt((double)variance);
        var lowerQuartile = Quartile(samples, upper: false);
        var upperQuartile = Quartile(samples, upper: true);

        return new CarryStatistics
        {
            MeanCarry = Distance.FromYards(mean),
            MedianCarry = Distance.FromYards(median),
            MinimumCarry = Distance.FromYards(minimum),
            MaximumCarry = Distance.FromYards(maximum),
            CarryRange = Distance.FromYards(maximum - minimum),
            CarryStandardDeviation = Distance.FromYards(standardDeviation),
            CoefficientOfVariation = mean > 0m ? standardDeviation / mean : 0m,
            InterquartileRange = Distance.FromYards(upperQuartile - lowerQuartile),
            SampleCount = samples.Length,
        };
    }

    private static decimal Median(decimal[] sortedValues)
    {
        if (sortedValues.Length == 1)
        {
            return sortedValues[0];
        }

        var midpoint = sortedValues.Length / 2;
        if (sortedValues.Length % 2 == 0)
        {
            return (sortedValues[midpoint - 1] + sortedValues[midpoint]) / 2m;
        }

        return sortedValues[midpoint];
    }

    private static decimal Quartile(decimal[] sortedValues, bool upper)
    {
        if (sortedValues.Length <= 1)
        {
            return sortedValues.Length == 0 ? 0m : sortedValues[0];
        }

        if (sortedValues.Length == 2)
        {
            return upper ? sortedValues[1] : sortedValues[0];
        }

        if (sortedValues.Length % 2 == 0)
        {
            return Median(upper
                ? sortedValues.Skip(sortedValues.Length / 2).ToArray()
                : sortedValues.Take(sortedValues.Length / 2).ToArray());
        }

        var half = sortedValues.Length / 2;
        return Median(upper
            ? sortedValues.Skip(half + 1).ToArray()
            : sortedValues.Take(half).ToArray());
    }
}
