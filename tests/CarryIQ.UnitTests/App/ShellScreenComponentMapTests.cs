using CarryIQ.App;
using CarryIQ.App.Components.Pages;
using CarryIQ.App.Components.Shared;
using CarryIQ.App.Components.Shell;

namespace CarryIQ.UnitTests.App;

public class ShellScreenComponentMapTests
{
    [Theory]
    [InlineData(typeof(DashboardViewModel), typeof(DashboardPage))]
    [InlineData(typeof(ClubManagerViewModel), typeof(ClubManagerPage))]
    [InlineData(typeof(SessionManagerViewModel), typeof(SessionManagerPage))]
    [InlineData(typeof(ShotEntryViewModel), typeof(ShotEntryPage))]
    [InlineData(typeof(ShotReviewViewModel), typeof(ShotReviewPage))]
    [InlineData(typeof(UtilitiesViewModel), typeof(UtilitiesPage))]
    [InlineData(typeof(WedgeMatrixViewModel), typeof(WedgeMatrixPage))]
    [InlineData(typeof(AnalyticsViewModel), typeof(AnalyticsPage))]
    [InlineData(typeof(PlaceholderScreenViewModel), typeof(PlaceholderScreen))]
    public void ResolveReturnsTheExpectedComponentForEachScreenViewModel(Type screenType, Type expectedComponentType)
    {
        Assert.Equal(expectedComponentType, ShellScreenComponentMap.Resolve(screenType));
    }
}
