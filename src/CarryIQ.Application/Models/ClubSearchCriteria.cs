namespace CarryIQ.Application;

public sealed record ClubSearchCriteria(
    Guid? GolferProfileId = null,
    bool? ActiveOnly = null,
    string? SearchText = null);
