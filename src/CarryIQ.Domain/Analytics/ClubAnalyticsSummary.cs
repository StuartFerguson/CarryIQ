namespace CarryIQ.Domain;

public sealed record ClubAnalyticsSummary
{
    public required Guid? ClubId { get; init; }

    public required string ClubName { get; init; }

    public required CarryStatistics Statistics { get; init; }

    public required decimal RepresentativeCarryYards { get; init; }

    public required decimal ConsistencyScore { get; init; }

    public required int SampleWarningThreshold { get; init; }

    public required bool HasInsufficientSamples { get; init; }
}
