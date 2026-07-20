using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CarryIQ.App;

namespace CarryIQ.UnitTests.App;

public class ShotEntryViewModelTests
{
    [Fact]
    public async Task InitializeAsyncSelectsPersistedLastClubAndLatestSession()
    {
        var sessionOld = CreateSessionSummary(DateOnly.Parse("2026-07-18", CultureInfo.InvariantCulture), "Old range");
        var sessionNew = CreateSessionSummary(DateOnly.Parse("2026-07-19", CultureInfo.InvariantCulture), "New range");
        var clubA = CreateClubSummary("5 Iron", 1);
        var clubB = CreateClubSummary("7 Iron", 2);
        var store = new TestShotEntryPreferencesStore { LastClubId = clubB.Id };

        var viewModel = CreateViewModel(
            [clubA, clubB],
            [sessionOld, sessionNew],
            store);

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(sessionNew.Id, viewModel.SelectedSessionId);
        Assert.Equal(clubB.Id, viewModel.SelectedClubId);
    }

    [Fact]
    public async Task SaveCommandPersistsShotAndLastClub()
    {
        var session = CreateSessionSummary(DateOnly.Parse("2026-07-19", CultureInfo.InvariantCulture), "Morning range");
        var club = CreateClubSummary("7 Iron", 1);
        var store = new TestShotEntryPreferencesStore();
        var shots = new TestShotRepository();

        var viewModel = CreateViewModel(
            [club],
            [session],
            store,
            shots);

        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.CarryDistanceText = "154";
        viewModel.TotalDistanceText = "162";
        viewModel.BallSpeedText = "118";
        viewModel.ClubSpeedText = "87";
        viewModel.LaunchAngleText = "13.8";
        viewModel.Notes = "Pure strike";

        await viewModel.SaveCommand.ExecuteAsync(null);

        var saved = Assert.Single(shots.AddedShots);
        Assert.Equal(session.Id, saved.PracticeSessionId);
        Assert.Equal(club.Id, saved.ClubId);
        Assert.Equal(ShotSourceKind.Manual, saved.Source);
        Assert.Equal(Distance.FromYards(154m), saved.CarryDistance);
        Assert.Equal(Distance.FromYards(162m), saved.TotalDistance);
        Assert.Equal(Speed.FromMilesPerHour(118m), saved.BallSpeed);
        Assert.Equal(Speed.FromMilesPerHour(87m), saved.ClubSpeed);
        Assert.Equal(13.8m, saved.LaunchAngle);
        Assert.Equal("Pure strike", saved.Notes);
        Assert.Equal(club.Id, store.LastSavedClubId);
        Assert.Equal(club.Id, viewModel.SelectedClubId);
        Assert.Equal(string.Empty, viewModel.CarryDistanceText);
        Assert.Equal(string.Empty, viewModel.TotalDistanceText);
        Assert.Equal(string.Empty, viewModel.BallSpeedText);
        Assert.Equal(string.Empty, viewModel.ClubSpeedText);
        Assert.Equal(string.Empty, viewModel.LaunchAngleText);
    }

    [Fact]
    public async Task InvalidNumericInputSurfacesFieldLevelError()
    {
        var session = CreateSessionSummary(DateOnly.Parse("2026-07-19", CultureInfo.InvariantCulture), "Morning range");
        var club = CreateClubSummary("7 Iron", 1);
        var viewModel = CreateViewModel([club], [session]);

        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.CarryDistanceText = "abc";

        var errors = viewModel.GetErrors(nameof(ShotEntryViewModel.CarryDistanceText)).Cast<string>().ToList();

        Assert.Contains(errors, error => error.Contains("carry distance", StringComparison.OrdinalIgnoreCase));
        Assert.False(viewModel.SaveCommand.CanExecute(null));
    }

    private static ShotEntryViewModel CreateViewModel(
        IReadOnlyList<ClubSummary> clubs,
        IReadOnlyList<PracticeSessionSummary> sessions,
        TestShotEntryPreferencesStore? store = null,
        TestShotRepository? shots = null)
    {
        return new ShotEntryViewModel(
            new TestClubRepository(clubs),
            new TestPracticeSessionRepository(sessions),
            shots ?? new TestShotRepository(),
            store ?? new TestShotEntryPreferencesStore());
    }

    private static ClubSummary CreateClubSummary(string name, int sortOrder) =>
        new(Guid.NewGuid(), name, ClubType.Iron, sortOrder, true);

    private static PracticeSessionSummary CreateSessionSummary(DateOnly sessionDate, string name) =>
        new(Guid.NewGuid(), Guid.NewGuid(), name, sessionDate, SessionType.DrivingRange, "Range", "Trackman", 0, 0);

    private sealed class TestClubRepository(IReadOnlyList<ClubSummary> clubs) : IClubRepository
    {
        public Task<Club?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Club?>(null);

        public Task<IReadOnlyList<ClubSummary>> SearchAsync(ClubSearchCriteria criteria, CancellationToken cancellationToken) =>
            Task.FromResult(clubs);

        public Task SaveAsync(Club club, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestPracticeSessionRepository(IReadOnlyList<PracticeSessionSummary> sessions) : IPracticeSessionRepository
    {
        public Task<PracticeSession?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<PracticeSession?>(null);

        public Task<IReadOnlyList<PracticeSessionSummary>> SearchAsync(SessionSearchCriteria criteria, CancellationToken cancellationToken) =>
            Task.FromResult(sessions);

        public Task SaveAsync(PracticeSession session, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TestShotRepository : IShotRepository
    {
        public List<Shot> AddedShots { get; } = [];

        public Task AddAsync(Shot shot, CancellationToken cancellationToken)
        {
            AddedShots.Add(shot);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IReadOnlyCollection<Shot> shots, CancellationToken cancellationToken)
        {
            AddedShots.AddRange(shots);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Shot shot, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<Shot>> SearchAsync(ShotSearchCriteria criteria, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Shot>>([]);
    }

    private sealed class TestShotEntryPreferencesStore : IShotEntryPreferencesStore
    {
        public Guid? LastSavedClubId { get; private set; }

        public Guid? LastClubId { get; set; }

        public Task<ShotEntryPreferences> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new ShotEntryPreferences { LastClubId = LastClubId });

        public Task SaveAsync(ShotEntryPreferences preferences, CancellationToken cancellationToken)
        {
            LastSavedClubId = preferences.LastClubId;
            LastClubId = preferences.LastClubId;
            return Task.CompletedTask;
        }
    }
}
