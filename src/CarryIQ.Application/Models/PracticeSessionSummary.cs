namespace CarryIQ.Application;

public sealed record PracticeSessionSummary(
    Guid Id,
    Guid GolferProfileId,
    string Name,
    DateOnly SessionDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    TimeSpan? Duration,
    SessionType SessionType,
    string? LocationName,
    string? LaunchMonitorSource,
    int ShotCount,
    int ValidShotCount,
    bool IsArchived)
{
    public string DisplayDuration =>
        Duration is null
            ? string.Empty
            : $"{(int)Duration.Value.TotalHours:00}:{Duration.Value.Minutes:00}";

    public string DisplayLocationName => string.IsNullOrWhiteSpace(LocationName) ? "-" : LocationName!;

    public string DisplayLaunchMonitorSource => string.IsNullOrWhiteSpace(LaunchMonitorSource) ? "-" : LaunchMonitorSource!;
}
