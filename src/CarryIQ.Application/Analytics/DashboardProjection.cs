namespace CarryIQ.Application;

public sealed record DashboardProjection(
    DashboardMetrics Metrics,
    IReadOnlyList<RecentSessionSummary> RecentSessions);
