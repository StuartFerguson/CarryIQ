using System.Globalization;

namespace CarryIQ.UnitTests.Domain;

public class DashboardProjectionCalculatorTests
{
    [Fact]
    public void CalculateBuildsDashboardProjectionFromIncludedShotsAndRecentSessions()
    {
        var golferProfileId = Guid.NewGuid();
        var olderSession = CreateSession(golferProfileId, "Evening range", new DateOnly(2026, 7, 18), 2, 1);
        var newerSession = CreateSession(golferProfileId, "Morning range", new DateOnly(2026, 7, 19), 2, 1);
        var shots = new[]
        {
            CreateShot(newerSession.Id, 148m, 150m, 2m, -2m, true),
            CreateShot(olderSession.Id, 152m, 150m, 2m, 2m, true),
            CreateShot(olderSession.Id, 161m, 155m, 3m, 4m, false),
        };

        var projection = DashboardProjectionCalculator.Calculate(
            shots,
            [olderSession, newerSession],
            DominantHand.Right,
            recentSessionCount: 2);

        Assert.Equal(2, projection.Metrics.SampleSize);
        Assert.Equal(150m, projection.Metrics.AverageCarryYards);
        Assert.Equal(2m, projection.Metrics.CarryStandardDeviationYards);
        Assert.Equal(2m, projection.Metrics.OfflineSpreadYards);
        Assert.Equal(0m, projection.Metrics.LeftRightBiasYards);
        Assert.Equal(0m, projection.Metrics.LongShortBiasYards);
        Assert.Equal(2, projection.RecentSessions.Count);
        Assert.Equal(newerSession.Id, projection.RecentSessions[0].SessionId);
        Assert.Equal(olderSession.Id, projection.RecentSessions[1].SessionId);
        Assert.Equal(1, projection.RecentSessions[0].IncludedShotCount);
        Assert.Equal(1, projection.RecentSessions[1].IncludedShotCount);
    }

    [Fact]
    public void CalculateFlipsLeftRightBiasForLeftHandedGolfers()
    {
        var session = CreateSession(Guid.NewGuid(), "Range", new DateOnly(2026, 7, 19), 1, 1);
        var shots = new[]
        {
            CreateShot(session.Id, 150m, 150m, 2m, 4m, true),
        };

        var rightHanded = DashboardProjectionCalculator.Calculate(shots, [session], DominantHand.Right, 1);
        var leftHanded = DashboardProjectionCalculator.Calculate(shots, [session], DominantHand.Left, 1);

        Assert.True(rightHanded.Metrics.LeftRightBiasYards > 0m);
        Assert.True(leftHanded.Metrics.LeftRightBiasYards < 0m);
    }

    private static PracticeSessionSummary CreateSession(
        Guid golferProfileId,
        string name,
        DateOnly sessionDate,
        int shotCount,
        int validShotCount) =>
        new(
            Guid.NewGuid(),
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
}
