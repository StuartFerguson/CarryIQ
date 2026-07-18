namespace CarryIQ.Application;

public sealed record PracticeSessionSummary(
    Guid Id,
    Guid GolferProfileId,
    string Name,
    DateOnly SessionDate,
    SessionType SessionType,
    string? LocationName,
    string? LaunchMonitorSource,
    int ShotCount,
    int ValidShotCount);
