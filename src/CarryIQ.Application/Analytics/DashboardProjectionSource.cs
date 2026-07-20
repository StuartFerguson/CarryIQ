namespace CarryIQ.Application;

public sealed record DashboardProjectionSource(
    IReadOnlyList<Shot> Shots,
    IReadOnlyList<PracticeSessionSummary> RecentSessions);
