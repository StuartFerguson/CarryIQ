using CarryIQ.App.Components.Pages;
using CarryIQ.App.Components.Shared;
namespace CarryIQ.App.Components.Shell;

public static class ShellScreenComponentMap
{
    public static Type Resolve(Type? screenType) => screenType switch
    {
        null => typeof(PlaceholderScreen),
        Type t when t == typeof(DashboardViewModel) => typeof(DashboardPage),
        Type t when t == typeof(ClubManagerViewModel) => typeof(ClubManagerPage),
        Type t when t == typeof(SessionManagerViewModel) => typeof(SessionManagerPage),
        Type t when t == typeof(ShotEntryViewModel) => typeof(ShotEntryPage),
        Type t when t == typeof(ShotReviewViewModel) => typeof(ShotReviewPage),
        Type t when t == typeof(UtilitiesViewModel) => typeof(UtilitiesPage),
        Type t when t == typeof(WedgeMatrixViewModel) => typeof(WedgeMatrixPage),
        Type t when t == typeof(AnalyticsViewModel) => typeof(AnalyticsPage),
        Type t when t == typeof(PlaceholderScreenViewModel) => typeof(PlaceholderScreen),
        _ => typeof(PlaceholderScreen),
    };
}
