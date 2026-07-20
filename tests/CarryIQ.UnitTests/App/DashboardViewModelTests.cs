using System.Globalization;
using CarryIQ.App;

namespace CarryIQ.UnitTests.App;

public class DashboardViewModelTests
{
    [Fact]
    public async Task InitializeAsyncLoadsMetricCardsAndRecentSessions()
    {
        var golferProfileId = Guid.NewGuid();
        var source = CreateSource();
        var viewModel = CreateViewModel(golferProfileId, source);

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal("Dashboard", viewModel.Title);
        Assert.Equal("A performance-first summary of carry, consistency, bias, and recent practice sessions.", viewModel.Summary);
        Assert.Equal(6, viewModel.MetricCards.Count);
        Assert.Contains(viewModel.MetricCards, card => card.Title == "Average carry");
        Assert.Contains(viewModel.MetricCards, card => card.Title == "Long/short bias");
        Assert.Equal(2, viewModel.RecentSessions.Count);
        Assert.Equal("Morning range", viewModel.SelectedSession?.Name);
        Assert.Equal("150 yd", viewModel.SelectedSession?.AverageCarryText);
    }

    [Fact]
    public async Task RefreshCommandPreservesSelectedSessionWhenItStillExists()
    {
        var golferProfileId = Guid.NewGuid();
        var repository = new TestDashboardProjectionRepository(CreateSource());
        var viewModel = new DashboardViewModel(
            repository,
            golferProfileId: golferProfileId,
            dominantHand: DominantHand.Right);

        await viewModel.InitializeAsync(CancellationToken.None);
        var selectedSessionId = viewModel.SelectedSession!.SessionId;

        repository.Source = CreateSource(selectedSessionId);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(selectedSessionId, viewModel.SelectedSession?.SessionId);
    }

    private static DashboardViewModel CreateViewModel(Guid golferProfileId, DashboardProjectionSource source)
    {
        return new DashboardViewModel(
            new TestDashboardProjectionRepository(source),
            golferProfileId: golferProfileId,
            dominantHand: DominantHand.Right);
    }

    private static DashboardProjectionSource CreateSource(Guid? selectedSessionId = null)
    {
        var golferProfileId = Guid.NewGuid();
        var newerSession = CreateSession(golferProfileId, selectedSessionId ?? Guid.NewGuid(), "Morning range", new DateOnly(2026, 7, 19), 2, 2);
        var olderSession = CreateSession(golferProfileId, Guid.NewGuid(), "Evening range", new DateOnly(2026, 7, 18), 1, 1);
        var shots = new[]
        {
            CreateShot(newerSession.Id, 148m, 150m, 2m, -2m, true),
            CreateShot(newerSession.Id, 152m, 150m, 2m, 2m, true),
            CreateShot(olderSession.Id, 150m, 150m, 2m, -1m, true),
        };

        return new DashboardProjectionSource(shots, [olderSession, newerSession]);
    }

    private static PracticeSessionSummary CreateSession(
        Guid golferProfileId,
        Guid sessionId,
        string name,
        DateOnly sessionDate,
        int shotCount,
        int validShotCount) =>
        new(
            sessionId,
            golferProfileId,
            name,
            sessionDate,
            null,
            null,
            null,
            SessionType.DrivingRange,
            "South Range",
            "Trackman",
            shotCount,
            validShotCount,
            false);

    private static Shot CreateShot(
        Guid sessionId,
        decimal carryYards,
        decimal targetYards,
        decimal offlineYards,
        decimal launchDirection,
        bool isIncluded) =>
        new()
        {
            Id = Guid.NewGuid(),
            PracticeSessionId = sessionId,
            ClubId = Guid.NewGuid(),
            ShotSequence = 1,
            RecordedAt = DateTimeOffset.Parse("2026-07-19T10:00:00Z", CultureInfo.InvariantCulture),
            Source = ShotSourceKind.Manual,
            CarryDistance = Distance.FromYards(carryYards),
            TotalDistance = Distance.FromYards(carryYards + 8m),
            BallSpeed = Speed.FromMilesPerHour(111m),
            ClubSpeed = Speed.FromMilesPerHour(83m),
            SmashFactor = 1.34m,
            LaunchAngle = 14.2m,
            LaunchDirection = launchDirection,
            ApexHeight = 28m,
            SpinRate = 6200m,
            SpinAxis = -2m,
            OfflineDistance = Distance.FromYards(offlineYards),
            RollDistance = Distance.FromYards(8m),
            HangTime = 4.8m,
            AttackAngle = -4.2m,
            ClubPath = 1.2m,
            FaceAngle = 0.4m,
            FaceToPath = -0.8m,
            DynamicLoft = 23m,
            StrikeQuality = StrikeQuality.Good,
            ShotShape = ShotShape.Draw,
            LieType = "Tee",
            SwingType = SwingType.Full,
            TargetDistance = Distance.FromYards(targetYards),
            IsIncluded = isIncluded,
            ExclusionReason = isIncluded ? null : "Example exclusion",
            IsEstimated = false,
            Notes = "Seed shot",
            RawImportData = null,
            CreatedAt = DateTimeOffset.Parse("2026-07-19T10:00:00Z", CultureInfo.InvariantCulture),
            UpdatedAt = DateTimeOffset.Parse("2026-07-19T10:00:00Z", CultureInfo.InvariantCulture),
        };

    private sealed class TestDashboardProjectionRepository(DashboardProjectionSource source) : IDashboardProjectionRepository
    {
        public DashboardProjectionSource Source { get; set; } = source;

        public Task<DashboardProjectionSource> LoadAsync(Guid golferProfileId, int recentSessionCount, CancellationToken cancellationToken) =>
            Task.FromResult(Source);
    }
}
