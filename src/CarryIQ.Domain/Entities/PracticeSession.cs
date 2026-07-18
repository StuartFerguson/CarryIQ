namespace CarryIQ.Domain;

public sealed record PracticeSession
{
    public required Guid Id { get; init; }

    public required Guid GolferProfileId { get; init; }

    public required string Name { get; init; }

    public required DateOnly SessionDate { get; init; }

    public TimeOnly? StartTime { get; init; }

    public TimeOnly? EndTime { get; init; }

    public string? LocationName { get; init; }

    public required SessionType SessionType { get; init; }

    public required SurfaceType SurfaceType { get; init; }

    public string? BallType { get; init; }

    public string? LaunchMonitorSource { get; init; }

    public string? WeatherDescription { get; init; }

    public decimal? TemperatureCelsius { get; init; }

    public Speed? WindSpeed { get; init; }

    public string? WindDirection { get; init; }

    public decimal? ElevationMetres { get; init; }

    public string? Notes { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
