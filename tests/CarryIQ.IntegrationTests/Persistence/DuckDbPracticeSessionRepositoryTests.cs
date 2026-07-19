namespace CarryIQ.IntegrationTests.Persistence;

public class DuckDbPracticeSessionRepositoryTests
{
    [Fact]
    public async Task SaveAndGetPracticeSessionRoundTripsAllFields()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var session = new PracticeSession
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            GolferProfileId = scope.DefaultGolferProfileId,
            Name = "Morning Range",
            SessionDate = new DateOnly(2026, 7, 19),
            StartTime = new TimeOnly(9, 15),
            EndTime = new TimeOnly(10, 30),
            LocationName = "South Range",
            SessionType = SessionType.DrivingRange,
            SurfaceType = SurfaceType.Grass,
            BallType = "Titleist Pro V1",
            LaunchMonitorSource = "Trackman",
            WeatherDescription = "Calm and clear",
            TemperatureCelsius = 21.5m,
            WindSpeed = Speed.FromMilesPerHour(8m),
            WindDirection = "NW",
            ElevationMetres = 42m,
            Notes = "Long game focus",
            IsArchived = false,
            CreatedAt = DateTimeOffset.Parse("2026-07-19T09:15:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2026-07-19T10:30:00Z"),
        };

        await scope.Sessions.SaveAsync(session, CancellationToken.None);

        var loaded = await scope.Sessions.GetAsync(session.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(session, loaded);
    }

    [Fact]
    public async Task SearchSessionsReturnsShotCounts()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var sessionId = await scope.SeedPracticeSessionAsync();
        var clubId = await scope.SeedClubAsync("7 Iron", ClubType.Iron, isActive: true, sortOrder: 4);
        await scope.SeedShotAsync(sessionId, clubId, isIncluded: true);
        await scope.SeedShotAsync(sessionId, clubId, isIncluded: false);

        var results = await scope.Sessions.SearchAsync(
            new SessionSearchCriteria(scope.DefaultGolferProfileId, SearchText: "range"),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(2, results[0].ShotCount);
        Assert.Equal(1, results[0].ValidShotCount);
    }

    [Fact]
    public async Task SearchSessionsFiltersByArchiveState()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var activeSessionId = await scope.SeedPracticeSessionAsync();
        var archivedSessionId = await scope.SeedPracticeSessionAsync(isArchived: true);

        var activeResults = await scope.Sessions.SearchAsync(
            new SessionSearchCriteria(scope.DefaultGolferProfileId, Archived: false),
            CancellationToken.None);
        var archivedResults = await scope.Sessions.SearchAsync(
            new SessionSearchCriteria(scope.DefaultGolferProfileId, Archived: true),
            CancellationToken.None);

        Assert.Contains(activeResults, session => session.Id == activeSessionId);
        Assert.DoesNotContain(activeResults, session => session.Id == archivedSessionId);
        Assert.Contains(archivedResults, session => session.Id == archivedSessionId);
        Assert.DoesNotContain(archivedResults, session => session.Id == activeSessionId);
    }

    [Fact]
    public async Task DeleteSessionRemovesRelatedShots()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var sessionId = await scope.SeedPracticeSessionAsync();
        var clubId = await scope.SeedClubAsync("7 Iron", ClubType.Iron, isActive: true, sortOrder: 4);
        await scope.SeedShotAsync(sessionId, clubId);

        await scope.Sessions.DeleteAsync(sessionId, CancellationToken.None);

        await using var connection = scope.OpenConnection();
        await connection.OpenAsync(CancellationToken.None);

        Assert.Equal(0L, await scope.ScalarAsync<long>(connection, $"SELECT COUNT(*) FROM PracticeSessions WHERE Id = '{sessionId}';"));
        Assert.Equal(0L, await scope.ScalarAsync<long>(connection, $"SELECT COUNT(*) FROM Shots WHERE PracticeSessionId = '{sessionId}';"));
    }
}
