namespace CarryIQ.Domain;

public sealed record WedgeSwingReference
{
    public required Guid Id { get; init; }

    public required Guid GolferProfileId { get; init; }

    public required Guid ClubId { get; init; }

    public required string SwingLabel { get; init; }

    public required SwingType SwingType { get; init; }

    public string? ClockPosition { get; init; }

    public Distance? TargetDistance { get; init; }

    public Distance? AverageCarry { get; init; }

    public Distance? CarryStandardDeviation { get; init; }

    public required int SampleSize { get; init; }

    public required bool IsManualOverride { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
