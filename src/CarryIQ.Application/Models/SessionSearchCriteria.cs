namespace CarryIQ.Application;

public sealed record SessionSearchCriteria(
    Guid? GolferProfileId = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    bool? Archived = null,
    string? SearchText = null);
