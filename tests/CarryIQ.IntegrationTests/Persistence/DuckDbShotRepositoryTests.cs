namespace CarryIQ.IntegrationTests.Persistence;

public class DuckDbShotRepositoryTests
{
    [Fact]
    public async Task AddAndSearchShotsPreservesMeasuredValues()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var sessionId = await scope.SeedPracticeSessionAsync();
        var clubId = await scope.SeedClubAsync("7 Iron", ClubType.Iron, isActive: true, sortOrder: 4);

        var shot = new Shot
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            PracticeSessionId = sessionId,
            ClubId = clubId,
            ShotSequence = 1,
            RecordedAt = DateTimeOffset.Parse("2026-07-19T10:10:00Z"),
            Source = ShotSourceKind.Manual,
            CarryDistance = Distance.FromYards(154m),
            TotalDistance = Distance.FromYards(162m),
            BallSpeed = Speed.FromMilesPerHour(118m),
            ClubSpeed = Speed.FromMilesPerHour(87m),
            SmashFactor = 1.36m,
            LaunchAngle = 13.8m,
            LaunchDirection = -0.6m,
            ApexHeight = 30m,
            SpinRate = 5900m,
            SpinAxis = -1.4m,
            OfflineDistance = Distance.FromYards(3m),
            RollDistance = Distance.FromYards(8m),
            HangTime = 4.9m,
            AttackAngle = -3.8m,
            ClubPath = 0.9m,
            FaceAngle = 0.2m,
            FaceToPath = -0.7m,
            DynamicLoft = 22.5m,
            StrikeQuality = StrikeQuality.Good,
            ShotShape = ShotShape.Draw,
            LieType = "Tee",
            SwingType = SwingType.Full,
            TargetDistance = Distance.FromYards(155m),
            IsIncluded = true,
            ExclusionReason = null,
            IsEstimated = false,
            Notes = "Pure strike",
            RawImportData = "{\"source\":\"manual\"}",
            CreatedAt = DateTimeOffset.Parse("2026-07-19T10:10:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2026-07-19T10:10:00Z"),
        };

        await scope.Shots.AddAsync(shot, CancellationToken.None);

        var results = await scope.Shots.SearchAsync(
            new ShotSearchCriteria(PracticeSessionId: sessionId, IncludedOnly: true, SearchText: "strike"),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(shot, results[0]);
    }

    [Fact]
    public async Task AddRangeAsyncInsertsMultipleShots()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var sessionId = await scope.SeedPracticeSessionAsync();
        var clubId = await scope.SeedClubAsync("7 Iron", ClubType.Iron, isActive: true, sortOrder: 4);

        var shot1 = await scope.SeedShotAsync(sessionId, clubId, isIncluded: true);
        var shot2 = await scope.SeedShotAsync(sessionId, clubId, isIncluded: false);

        await scope.Shots.AddRangeAsync([shot1, shot2], CancellationToken.None);

        var results = await scope.Shots.SearchAsync(new ShotSearchCriteria(PracticeSessionId: sessionId), CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].ShotSequence);
        Assert.Equal(1, results[1].ShotSequence);
    }

    [Fact]
    public async Task UpdateShotPersistsEditedFields()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var sessionId = await scope.SeedPracticeSessionAsync();
        var clubId = await scope.SeedClubAsync("7 Iron", ClubType.Iron, isActive: true, sortOrder: 4);
        var shot = await scope.SeedShotAsync(sessionId, clubId);

        shot = shot with
        {
            Notes = "Updated note",
            IsIncluded = false,
            UpdatedAt = DateTimeOffset.Parse("2026-07-19T10:15:00Z"),
        };

        await scope.Shots.UpdateAsync(shot, CancellationToken.None);

        var loaded = await scope.Shots.SearchAsync(new ShotSearchCriteria(PracticeSessionId: sessionId), CancellationToken.None);

        Assert.Single(loaded);
        Assert.Equal("Updated note", loaded[0].Notes);
        Assert.False(loaded[0].IsIncluded);
    }
}
