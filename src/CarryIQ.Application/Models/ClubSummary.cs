namespace CarryIQ.Application;

public sealed record ClubSummary(
    Guid Id,
    string Name,
    ClubType ClubType,
    int SortOrder,
    bool IsActive);
