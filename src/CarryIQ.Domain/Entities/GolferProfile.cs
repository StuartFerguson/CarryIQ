namespace CarryIQ.Domain;

public sealed record GolferProfile
{
    public required Guid Id { get; init; }

    public required string DisplayName { get; init; }

    public decimal? HandicapIndex { get; init; }

    public required DominantHand DominantHand { get; init; }

    public required DistanceUnit DefaultDistanceUnit { get; init; }

    public required SpeedUnit DefaultSpeedUnit { get; init; }

    public required TemperatureUnit DefaultTemperatureUnit { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
