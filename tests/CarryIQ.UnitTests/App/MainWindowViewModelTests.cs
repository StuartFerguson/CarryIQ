using CarryIQ.App;
using CarryIQ.Application;

namespace CarryIQ.UnitTests.App;

public class MainWindowViewModelTests
{
    [Fact]
    public void InitializesWithDashboardSelected()
    {
        var viewModel = new MainWindowViewModel(new TestApplicationPaths());

        Assert.Equal("CarryIQ", viewModel.ApplicationTitle);
        Assert.Equal("Dashboard", viewModel.SelectedNavigationItem?.Title);
        Assert.Equal("Dashboard", viewModel.CurrentScreen?.Title);
        Assert.Equal(11, viewModel.NavigationItems.Count);
    }

    [Fact]
    public void ChangingSelectionUpdatesTheCurrentScreen()
    {
        var viewModel = new MainWindowViewModel(new TestApplicationPaths());

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[4];

        Assert.Equal("Club Gapping", viewModel.SelectedNavigationItem?.Title);
        Assert.Equal("Club Gapping", viewModel.CurrentScreen?.Title);
        Assert.Equal("This page will evolve into the gapping analysis workspace.", viewModel.CurrentScreen?.Footer);
    }

    private sealed class TestApplicationPaths : IApplicationPaths
    {
        public string DataDirectory { get; } = @"C:\CarryIQ";

        public string DatabasePath { get; } = @"C:\CarryIQ\carryiq.duckdb";

        public string SettingsPath { get; } = @"C:\CarryIQ\user-settings.json";

        public string LogsDirectory { get; } = @"C:\CarryIQ\logs";

        public string BackupsDirectory { get; } = @"C:\CarryIQ\backups";
    }
}
