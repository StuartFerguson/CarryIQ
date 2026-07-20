using System.Collections.ObjectModel;
using System.Globalization;
using CarryIQ.App;

namespace CarryIQ.UnitTests.App;

public class AnalyticsViewModelTests
{
    [Fact]
    public async Task InitializeAsyncLoadsIncludedShotsIntoClubRows()
    {
        var clubs = new[]
        {
            CreateClubSummary("7 Iron", 1),
            CreateClubSummary("8 Iron", 2),
        };
        var shots = new[]
        {
            CreateShot(clubs[0].Id, 1, 150m, true),
            CreateShot(clubs[0].Id, 2, 152m, true),
            CreateShot(clubs[0].Id, 3, 148m, true),
            CreateShot(clubs[1].Id, 1, 140m, true),
            CreateShot(clubs[1].Id, 2, 138m, true),
            CreateShot(clubs[1].Id, 3, 142m, true),
        };

        var viewModel = CreateViewModel(clubs, shots);

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(2, viewModel.Rows.Count);
        Assert.Equal("8 Iron", viewModel.Rows[0].ClubName);
        Assert.Equal("7 Iron", viewModel.Rows[1].ClubName);
        Assert.Equal(3, viewModel.Rows[0].SampleCount);
        Assert.True(viewModel.Rows[0].HasInsufficientSamples);
        Assert.Equal(10m, viewModel.Rows[0].GapToNextYards);
        Assert.Null(viewModel.Rows[1].GapToNextYards);
        Assert.Equal("8 Iron", viewModel.SelectedRow?.ClubName);
        Assert.Equal(ClubGapOption.Median, viewModel.SelectedGapOption);
    }

    [Fact]
    public async Task RefreshCommandUsesMeanGapOptionWhenSelected()
    {
        var clubs = new[]
        {
            CreateClubSummary("7 Iron", 1),
            CreateClubSummary("8 Iron", 2),
        };
        var shots = new[]
        {
            CreateShot(clubs[0].Id, 1, 100m, true),
            CreateShot(clubs[0].Id, 2, 100m, true),
            CreateShot(clubs[0].Id, 3, 130m, true),
            CreateShot(clubs[1].Id, 1, 90m, true),
            CreateShot(clubs[1].Id, 2, 90m, true),
            CreateShot(clubs[1].Id, 3, 90m, true),
        };

        var viewModel = CreateViewModel(clubs, shots);

        await viewModel.InitializeAsync(CancellationToken.None);
        viewModel.SelectedGapOption = ClubGapOption.Mean;

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(20m, viewModel.Rows[0].GapToNextYards);
        Assert.Equal("8 Iron", viewModel.Rows[0].ClubName);
    }

    private static AnalyticsViewModel CreateViewModel(
        IReadOnlyList<ClubSummary> clubs,
        IReadOnlyList<Shot> shots)
    {
        return new AnalyticsViewModel(
            new TestClubRepository(clubs),
            new TestShotRepository(shots));
    }

    private static ClubSummary CreateClubSummary(string name, int sortOrder) =>
        new(Guid.NewGuid(), name, ClubType.Iron, sortOrder, true);

    private static Shot CreateShot(Guid clubId, int sequence, decimal carryYards, bool included) =>
        new()
        {
            Id = Guid.NewGuid(),
            PracticeSessionId = Guid.NewGuid(),
            ClubId = clubId,
            ShotSequence = sequence,
            RecordedAt = DateTimeOffset.Parse($"2026-07-19T10:0{sequence}:00Z", CultureInfo.InvariantCulture),
            Source = ShotSourceKind.Manual,
            CarryDistance = Distance.FromYards(carryYards),
            TotalDistance = Distance.FromYards(carryYards + 10m),
            BallSpeed = Speed.FromMilesPerHour(110m),
            ClubSpeed = Speed.FromMilesPerHour(80m),
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
            TargetDistance = Distance.FromYards(carryYards + 5m),
            IsIncluded = included,
            ExclusionReason = included ? null : "Manual review",
            IsEstimated = false,
            Notes = $"Shot {sequence}",
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

    private sealed class TestShotRepository(IReadOnlyList<Shot> shots) : IShotRepository
    {
        public Task AddAsync(Shot shot, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task AddRangeAsync(IReadOnlyCollection<Shot> shots, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(Shot shot, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<Shot>> SearchAsync(ShotSearchCriteria criteria, CancellationToken cancellationToken)
        {
            IEnumerable<Shot> results = shots;

            if (criteria.IncludedOnly is bool includedOnly)
            {
                results = results.Where(shot => shot.IsIncluded == includedOnly);
            }

            return Task.FromResult<IReadOnlyList<Shot>>(results.ToList());
        }
    }
}
