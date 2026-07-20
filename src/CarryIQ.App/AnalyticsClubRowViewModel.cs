using CommunityToolkit.Mvvm.ComponentModel;

namespace CarryIQ.App;

public sealed partial class AnalyticsClubRowViewModel : ObservableObject
{
    private decimal? _gapToNextYards;
    private bool _hasGapWarning;
    private bool _hasOverlap;

    public AnalyticsClubRowViewModel(ClubSummary club, ClubAnalyticsSummary analytics)
    {
        ClubId = club.Id;
        ClubName = club.Name;
        ClubType = club.ClubType;
        SampleCount = analytics.Statistics.SampleCount;
        MeanCarryYards = analytics.Statistics.MeanCarry.Yards;
        MedianCarryYards = analytics.Statistics.MedianCarry.Yards;
        RepresentativeCarryYards = analytics.RepresentativeCarryYards;
        MinimumCarryYards = analytics.Statistics.MinimumCarry.Yards;
        MaximumCarryYards = analytics.Statistics.MaximumCarry.Yards;
        CarryRangeYards = analytics.Statistics.CarryRange.Yards;
        StandardDeviationYards = analytics.Statistics.CarryStandardDeviation.Yards;
        CoefficientOfVariation = analytics.Statistics.CoefficientOfVariation;
        InterquartileRangeYards = analytics.Statistics.InterquartileRange.Yards;
        ConsistencyScore = analytics.ConsistencyScore;
        SampleWarningThreshold = analytics.SampleWarningThreshold;
        HasInsufficientSamples = analytics.HasInsufficientSamples;
        SampleWarningText = analytics.HasInsufficientSamples
            ? $"Only {SampleCount} shots; minimum recommended sample is {SampleWarningThreshold}."
            : string.Empty;
    }

    public Guid ClubId { get; }

    public string ClubName { get; }

    public ClubType ClubType { get; }

    public int SampleCount { get; }

    public decimal MeanCarryYards { get; }

    public decimal MedianCarryYards { get; }

    public decimal RepresentativeCarryYards { get; }

    public decimal MinimumCarryYards { get; }

    public decimal MaximumCarryYards { get; }

    public decimal CarryRangeYards { get; }

    public decimal StandardDeviationYards { get; }

    public decimal CoefficientOfVariation { get; }

    public decimal InterquartileRangeYards { get; }

    public decimal ConsistencyScore { get; }

    public int SampleWarningThreshold { get; }

    public bool HasInsufficientSamples { get; }

    public string SampleWarningText { get; }

    public decimal? GapToNextYards
    {
        get => _gapToNextYards;
        internal set
        {
            if (SetProperty(ref _gapToNextYards, value))
            {
                OnPropertyChanged(nameof(GapStatusText));
            }
        }
    }

    public bool HasGapWarning
    {
        get => _hasGapWarning;
        internal set
        {
            if (SetProperty(ref _hasGapWarning, value))
            {
                OnPropertyChanged(nameof(GapStatusText));
            }
        }
    }

    public bool HasOverlap
    {
        get => _hasOverlap;
        internal set
        {
            if (SetProperty(ref _hasOverlap, value))
            {
                OnPropertyChanged(nameof(GapStatusText));
            }
        }
    }

    public string GapStatusText =>
        GapToNextYards is null
            ? "No next club"
            : HasOverlap
                ? "Overlap"
                : HasGapWarning
                    ? "Tight gap"
                    : "Healthy gap";
}
