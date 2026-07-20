namespace CarryIQ.Application;

public sealed record RecentSessionSummary(
    Guid SessionId,
    DateOnly SessionDate,
    string Name,
    int TotalShots,
    int IncludedShotCount,
    decimal? AverageCarryYards);
