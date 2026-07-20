namespace CarryIQ.Application;

public sealed record SessionSearchCriteria(
    Guid? GolferProfileId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    SessionType? SessionType = null,
    string? LaunchMonitorSource = null,
    bool? Archived = null,
    string? SearchText = null);
