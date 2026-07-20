namespace CarryIQ.App;

public sealed record DashboardMetricCardViewModel(
    string Title,
    string ValueText,
    string? DetailText = null);
