namespace CarryIQ.Domain;

public sealed record ClubAnalyticsResult
{
    public required IReadOnlyList<ClubAnalyticsSummary> Clubs { get; init; }

    public required IReadOnlyList<ClubGapSummary> Gaps { get; init; }

    public required ClubGapOption GapOption { get; init; }

    public required int SampleWarningThreshold { get; init; }

    public required decimal MinimumGapWarningYards { get; init; }
}
