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
        Assert.Equal(12, viewModel.NavigationItems.Count);
    }

    [Fact]
    public void ChangingSelectionUpdatesTheCurrentScreen()
    {
        var viewModel = new MainWindowViewModel(new TestApplicationPaths());

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[5];

        Assert.Equal("Club Gapping", viewModel.SelectedNavigationItem?.Title);
        Assert.Equal("Club Gapping", viewModel.CurrentScreen?.Title);
        Assert.Equal("This page will evolve into the gapping analysis workspace.", viewModel.CurrentScreen?.Footer);
    }

    [Fact]
    public void ChangingSelectionUsesTheWedgeMatrixScreenWhenAvailable()
    {
        var wedgeMatrix = new WedgeMatrixViewModel(
            new EmptyClubRepository(),
            new EmptyWedgeSwingReferenceRepository(),
            golferProfileId: Guid.NewGuid());
        var viewModel = new MainWindowViewModel(new TestApplicationPaths(), wedgeMatrix: wedgeMatrix);

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[6];

        Assert.Equal("Wedge Matrix", viewModel.SelectedNavigationItem?.Title);
        Assert.Equal("Wedge Matrix", viewModel.CurrentScreen?.Title);
        Assert.Equal("Phase 1 is read-only. Toggle inactive wedges to compare the full bag without editing references yet.", viewModel.CurrentScreen?.Footer);
    }

    [Fact]
    public void ChangingSelectionUsesTheAnalyticsScreenWhenAvailable()
    {
        var analytics = new AnalyticsViewModel(new EmptyClubRepository(), new EmptyShotRepository());
        var viewModel = new MainWindowViewModel(new TestApplicationPaths(), analytics: analytics);

        viewModel.SelectedNavigationItem = viewModel.NavigationItems[5];

        Assert.Equal("Club Gapping", viewModel.SelectedNavigationItem?.Title);
        Assert.Equal("Club Gapping", viewModel.CurrentScreen?.Title);
        Assert.Equal("Included shots are analysed by default. Switch between median and mean gaps to compare spacing.", viewModel.CurrentScreen?.Footer);
    }

    private sealed class TestApplicationPaths : IApplicationPaths
    {
        public string DataDirectory { get; } = @"C:\CarryIQ";

        public string DatabasePath { get; } = @"C:\CarryIQ\carryiq.duckdb";

        public string SettingsPath { get; } = @"C:\CarryIQ\user-settings.json";

        public string LogsDirectory { get; } = @"C:\CarryIQ\logs";

        public string BackupsDirectory { get; } = @"C:\CarryIQ\backups";
    }

    private sealed class EmptyClubRepository : IClubRepository
    {
        public Task<Club?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Club?>(null);

        public Task<IReadOnlyList<ClubSummary>> SearchAsync(ClubSearchCriteria criteria, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ClubSummary>>([]);

        public Task SaveAsync(Club club, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class EmptyShotRepository : IShotRepository
    {
        public Task AddAsync(Shot shot, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task AddRangeAsync(IReadOnlyCollection<Shot> shots, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(Shot shot, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<Shot>> SearchAsync(ShotSearchCriteria criteria, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Shot>>([]);
    }

    private sealed class EmptyWedgeSwingReferenceRepository : IWedgeSwingReferenceRepository
    {
        public Task<IReadOnlyList<WedgeSwingReference>> SearchAsync(Guid golferProfileId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WedgeSwingReference>>([]);
    }
}
