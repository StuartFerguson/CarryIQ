namespace CarryIQ.Domain;

public sealed record CarryStatistics
{
    public required Distance MeanCarry { get; init; }

    public required Distance MedianCarry { get; init; }

    public required Distance MinimumCarry { get; init; }

    public required Distance MaximumCarry { get; init; }

    public required Distance CarryRange { get; init; }

    public required Distance CarryStandardDeviation { get; init; }

    public required decimal CoefficientOfVariation { get; init; }

    public required Distance InterquartileRange { get; init; }

    public required int SampleCount { get; init; }
}
