namespace CarryIQ.Domain;

public sealed record ClubGapSummary
{
    public required string LowerClubName { get; init; }

    public required string UpperClubName { get; init; }

    public required decimal LowerCarryYards { get; init; }

    public required decimal UpperCarryYards { get; init; }

    public required decimal GapYards { get; init; }

    public required bool HasOverlap { get; init; }

    public required bool HasWarning { get; init; }
}
