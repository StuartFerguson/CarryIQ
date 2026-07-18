namespace CarryIQ.Application;

public sealed record ShotSearchCriteria(
    Guid? PracticeSessionId = null,
    Guid? ClubId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    bool? IncludedOnly = null,
    string? SearchText = null);
