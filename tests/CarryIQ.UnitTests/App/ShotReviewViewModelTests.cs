using System.Collections.ObjectModel;
using System.Globalization;
using CarryIQ.App;

namespace CarryIQ.UnitTests.App;

public class ShotReviewViewModelTests
{
    [Fact]
    public async Task InitializeAsyncLoadsLatestSessionAndSelectsMostRecentShot()
    {
        var sessionOld = CreateSessionSummary(DateOnly.Parse("2026-07-18", CultureInfo.InvariantCulture), "Old range");
        var sessionNew = CreateSessionSummary(DateOnly.Parse("2026-07-19", CultureInfo.InvariantCulture), "New range");
        var club = CreateClubSummary("7 Iron", 1);
        var shots = new[]
        {
            CreateShot(sessionOld.Id, club.Id, 1, "old note"),
            CreateShot(sessionNew.Id, club.Id, 1, "first new note"),
            CreateShot(sessionNew.Id, club.Id, 2, "latest new note"),
        };

        var viewModel = CreateViewModel([club], [sessionOld, sessionNew], shots);

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(sessionNew.Id, viewModel.SelectedSessionId);
        Assert.Equal(2, viewModel.Shots[0].ShotSequence);
        Assert.Equal("latest new note", viewModel.SelectedShot?.Notes);
        Assert.Equal("{\"source\":\"manual\"}", viewModel.SelectedShot?.RawImportData);
    }

    [Fact]
    public async Task SearchTextFiltersVisibleShots()
    {
        var session = CreateSessionSummary(DateOnly.Parse("2026-07-19", CultureInfo.InvariantCulture), "Morning range");
        var club = CreateClubSummary("7 Iron", 1);
        var shots = new[]
        {
            CreateShot(session.Id, club.Id, 1, "heel strike"),
            CreateShot(session.Id, club.Id, 2, "toe strike"),
        };

        var viewModel = CreateViewModel([club], [session], shots);

        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SearchText = "toe";
        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Single(viewModel.Shots);
        Assert.Equal("toe strike", viewModel.Shots[0].Notes);
    }

    [Fact]
    public async Task BulkExcludeCommandUpdatesSelectedShots()
    {
        var session = CreateSessionSummary(DateOnly.Parse("2026-07-19", CultureInfo.InvariantCulture), "Morning range");
        var club = CreateClubSummary("7 Iron", 1);
        var shots = new[]
        {
            CreateShot(session.Id, club.Id, 1, "first"),
            CreateShot(session.Id, club.Id, 2, "second"),
        };
        var repository = new TestShotRepository(shots);

        var viewModel = CreateViewModel([club], [session], shots, repository);

        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.Shots[0].IsSelected = true;
        viewModel.Shots[1].IsSelected = true;

        await viewModel.ExcludeSelectedCommand.ExecuteAsync(null);

        Assert.All(repository.UpdatedShots, shot => Assert.False(shot.IsIncluded));
        Assert.All(repository.UpdatedShots, shot => Assert.Equal("Manual review", shot.ExclusionReason));
    }

    [Fact]
    public async Task ApplyClubAndSwingTypeToSelectedShotsPersistsEdits()
    {
        var session = CreateSessionSummary(DateOnly.Parse("2026-07-19", CultureInfo.InvariantCulture), "Morning range");
        var clubA = CreateClubSummary("7 Iron", 1);
        var clubB = CreateClubSummary("8 Iron", 2);
        var shots = new[]
        {
            CreateShot(session.Id, clubA.Id, 1, "first"),
        };
        var repository = new TestShotRepository(shots);

        var viewModel = CreateViewModel([clubA, clubB], [session], shots, repository);

        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.SelectedClubId = clubB.Id;
        viewModel.SelectedSwingType = SwingType.ThreeQuarter;
        viewModel.Shots[0].IsSelected = true;

        await viewModel.ApplyClubCommand.ExecuteAsync(null);
        await viewModel.ApplySwingTypeCommand.ExecuteAsync(null);

        Assert.Equal(2, repository.UpdatedShots.Count);
        Assert.Equal(clubB.Id, repository.UpdatedShots[0].ClubId);
        Assert.Equal(clubB.Id, repository.UpdatedShots[1].ClubId);
        Assert.Equal(SwingType.ThreeQuarter, repository.UpdatedShots[1].SwingType);
    }

    private static ShotReviewViewModel CreateViewModel(
        IReadOnlyList<ClubSummary> clubs,
        IReadOnlyList<PracticeSessionSummary> sessions,
        IReadOnlyList<Shot> shots,
        TestShotRepository? repository = null)
    {
        return new ShotReviewViewModel(
            new TestClubRepository(clubs),
            new TestPracticeSessionRepository(sessions),
            repository ?? new TestShotRepository(shots));
    }

    private static ClubSummary CreateClubSummary(string name, int sortOrder) =>
        new(Guid.NewGuid(), name, ClubType.Iron, sortOrder, true);

    private static PracticeSessionSummary CreateSessionSummary(DateOnly sessionDate, string name) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            sessionDate,
            StartTime: null,
            EndTime: null,
            Duration: null,
            SessionType.DrivingRange,
            "Range",
            "Trackman",
            0,
            0,
            false);

    private static Shot CreateShot(Guid sessionId, Guid clubId, int sequence, string note) =>
        new()
        {
            Id = Guid.NewGuid(),
            PracticeSessionId = sessionId,
            ClubId = clubId,
            ShotSequence = sequence,
            RecordedAt = DateTimeOffset.Parse($"2026-07-19T10:0{sequence}:00Z", CultureInfo.InvariantCulture),
            Source = ShotSourceKind.Manual,
            CarryDistance = Distance.FromYards(150m + sequence),
            TotalDistance = Distance.FromYards(160m + sequence),
            BallSpeed = Speed.FromMilesPerHour(110m + sequence),
            ClubSpeed = Speed.FromMilesPerHour(80m + sequence),
            SmashFactor = 1.34m,
            LaunchAngle = 13.5m,
            LaunchDirection = -1.2m,
            ApexHeight = 28m,
            SpinRate = 6000m,
            SpinAxis = -2m,
            OfflineDistance = Distance.FromYards(3m),
            RollDistance = Distance.FromYards(7m),
            HangTime = 4.7m,
            AttackAngle = -4.1m,
            ClubPath = 1m,
            FaceAngle = 0.1m,
            FaceToPath = -0.6m,
            DynamicLoft = 22m,
            StrikeQuality = StrikeQuality.Good,
            ShotShape = ShotShape.Draw,
            LieType = "Tee",
            SwingType = SwingType.Full,
            TargetDistance = Distance.FromYards(155m),
            IsIncluded = true,
            ExclusionReason = null,
            IsEstimated = false,
            Notes = note,
            RawImportData = "{\"source\":\"manual\"}",
            CreatedAt = DateTimeOffset.Parse("2026-07-19T10:00:00Z", CultureInfo.InvariantCulture),
            UpdatedAt = DateTimeOffset.Parse("2026-07-19T10:00:00Z", CultureInfo.InvariantCulture),
        };

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
        private readonly List<Shot> _shots;

        public TestShotRepository(IReadOnlyList<Shot> shots)
        {
            _shots = shots.ToList();
        }

        public List<Shot> UpdatedShots { get; } = [];

        public Task AddAsync(Shot shot, CancellationToken cancellationToken)
        {
            _shots.Add(shot);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IReadOnlyCollection<Shot> shots, CancellationToken cancellationToken)
        {
            _shots.AddRange(shots);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Shot shot, CancellationToken cancellationToken)
        {
            UpdatedShots.Add(shot);
            var index = _shots.FindIndex(item => item.Id == shot.Id);
            if (index >= 0)
            {
                _shots[index] = shot;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Shot>> SearchAsync(ShotSearchCriteria criteria, CancellationToken cancellationToken)
        {
            IEnumerable<Shot> results = _shots;
            if (criteria.PracticeSessionId is Guid sessionId)
            {
                results = results.Where(shot => shot.PracticeSessionId == sessionId);
            }

            if (criteria.ClubId is Guid clubId)
            {
                results = results.Where(shot => shot.ClubId == clubId);
            }

            if (criteria.IncludedOnly is bool includedOnly)
            {
                results = results.Where(shot => shot.IsIncluded == includedOnly);
            }

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var search = criteria.SearchText.Trim().ToLower(CultureInfo.InvariantCulture);
                results = results.Where(shot =>
                    (shot.Notes ?? string.Empty).ToLower(CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (shot.ExclusionReason ?? string.Empty).ToLower(CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (shot.LieType ?? string.Empty).ToLower(CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (shot.RawImportData ?? string.Empty).ToLower(CultureInfo.InvariantCulture).Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult<IReadOnlyList<Shot>>(results.ToList());
        }
    }
}
