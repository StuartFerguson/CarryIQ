namespace CarryIQ.Domain;

public static class ClubAnalyticsCalculator
{
    public static ClubAnalyticsResult Calculate(
        IEnumerable<(string ClubName, IEnumerable<Distance> Carries)> clubs,
        ClubGapOption gapOption,
        int sampleWarningThreshold = 5,
        decimal minimumGapWarningYards = 10m)
    {
        ArgumentNullException.ThrowIfNull(clubs);

        return CalculateCore(
            clubs.Select(club => new ClubAnalyticsSource(null, club.ClubName, club.Carries?.ToArray() ?? [])),
            gapOption,
            sampleWarningThreshold,
            minimumGapWarningYards);
    }

    public static ClubAnalyticsResult Calculate(
        IEnumerable<(Guid ClubId, string ClubName, IEnumerable<Distance> Carries)> clubs,
        ClubGapOption gapOption,
        int sampleWarningThreshold = 5,
        decimal minimumGapWarningYards = 10m)
    {
        ArgumentNullException.ThrowIfNull(clubs);

        return CalculateCore(
            clubs.Select(club => new ClubAnalyticsSource(club.ClubId, club.ClubName, club.Carries?.ToArray() ?? [])),
            gapOption,
            sampleWarningThreshold,
            minimumGapWarningYards);
    }

    private static ClubAnalyticsResult CalculateCore(
        IEnumerable<ClubAnalyticsSource> clubs,
        ClubGapOption gapOption,
        int sampleWarningThreshold,
        decimal minimumGapWarningYards)
    {
        var summaries = clubs.Select(club =>
        {
            var statistics = CarryStatisticsCalculator.Calculate(club.Carries);
            var representativeCarry = gapOption switch
            {
                ClubGapOption.Mean => statistics.MeanCarry.Yards,
                ClubGapOption.Median => statistics.MedianCarry.Yards,
                _ => statistics.MedianCarry.Yards,
            };

            return new ClubAnalyticsSummary
            {
                ClubId = club.ClubId,
                ClubName = club.ClubName,
                Statistics = statistics,
                RepresentativeCarryYards = representativeCarry,
                ConsistencyScore = ConsistencyScoreCalculator.Calculate(statistics),
                SampleWarningThreshold = sampleWarningThreshold,
                HasInsufficientSamples = statistics.SampleCount < sampleWarningThreshold,
            };
        })
        .OrderBy(summary => summary.RepresentativeCarryYards)
        .ThenBy(summary => summary.ClubName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

        var gaps = new List<ClubGapSummary>();
        for (var index = 1; index < summaries.Length; index++)
        {
            var lower = summaries[index - 1];
            var upper = summaries[index];
            var gapYards = upper.RepresentativeCarryYards - lower.RepresentativeCarryYards;

            gaps.Add(new ClubGapSummary
            {
                LowerClubName = lower.ClubName,
                UpperClubName = upper.ClubName,
                LowerCarryYards = lower.RepresentativeCarryYards,
                UpperCarryYards = upper.RepresentativeCarryYards,
                GapYards = gapYards,
                HasOverlap = gapYards <= 0m,
                HasWarning = gapYards < minimumGapWarningYards,
            });
        }

        return new ClubAnalyticsResult
        {
            Clubs = summaries,
            Gaps = gaps,
            GapOption = gapOption,
            SampleWarningThreshold = sampleWarningThreshold,
            MinimumGapWarningYards = minimumGapWarningYards,
        };
    }

    private sealed record ClubAnalyticsSource(Guid? ClubId, string ClubName, Distance[] Carries);
}
