namespace CarryIQ.IntegrationTests.Persistence;

public class DuckDbClubRepositoryTests
{
    [Fact]
    public async Task SaveAndGetClubRoundTripsAllFields()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var club = new Club
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            GolferProfileId = scope.DefaultGolferProfileId,
            Name = "Tour 7 Iron",
            ClubType = ClubType.Iron,
            Manufacturer = "Mizuno",
            Model = "JPX 923",
            Loft = 32m,
            Shaft = "Dynamic Gold",
            ShaftFlex = "S300",
            Length = Distance.FromYards(37m),
            IsActive = true,
            SortOrder = 4,
            Notes = "Benchmark club",
            CreatedAt = DateTimeOffset.Parse("2026-07-19T10:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2026-07-19T10:05:00Z"),
        };

        await scope.Clubs.SaveAsync(club, CancellationToken.None);

        var loaded = await scope.Clubs.GetAsync(club.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(club, loaded);
    }

    [Fact]
    public async Task SearchClubsFiltersByCriteria()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        await scope.Clubs.SaveAsync(
            new Club
            {
                Id = Guid.NewGuid(),
                GolferProfileId = scope.DefaultGolferProfileId,
                Name = "Utility Iron",
                ClubType = ClubType.UtilityIron,
                Manufacturer = "Mizuno",
                Model = "Fli-Hi",
                IsActive = true,
                SortOrder = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            CancellationToken.None);

        await scope.Clubs.SaveAsync(
            new Club
            {
                Id = Guid.NewGuid(),
                GolferProfileId = scope.DefaultGolferProfileId,
                Name = "Old 3 Wood",
                ClubType = ClubType.FairwayWood,
                Manufacturer = "TaylorMade",
                Model = "M6",
                IsActive = false,
                SortOrder = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            CancellationToken.None);

        var results = await scope.Clubs.SearchAsync(
            new ClubSearchCriteria(scope.DefaultGolferProfileId, ActiveOnly: true, SearchText: "mizuno"),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("Utility Iron", results[0].Name);
        Assert.True(results[0].IsActive);
    }

    [Fact]
    public async Task SearchClubsReturnsResultsInSortOrder()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        await scope.Clubs.SaveAsync(
            new Club
            {
                Id = Guid.NewGuid(),
                GolferProfileId = scope.DefaultGolferProfileId,
                Name = "Practice Driver",
                ClubType = ClubType.Driver,
                IsActive = true,
                SortOrder = 10,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            CancellationToken.None);

        await scope.Clubs.SaveAsync(
            new Club
            {
                Id = Guid.NewGuid(),
                GolferProfileId = scope.DefaultGolferProfileId,
                Name = "Practice Wedge",
                ClubType = ClubType.GapWedge,
                IsActive = true,
                SortOrder = 11,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            CancellationToken.None);

        var results = await scope.Clubs.SearchAsync(
            new ClubSearchCriteria(scope.DefaultGolferProfileId, ActiveOnly: true, SearchText: "practice"),
            CancellationToken.None);

        Assert.Equal(["Practice Driver", "Practice Wedge"], results.Select(item => item.Name));
        Assert.Equal([10, 11], results.Select(item => item.SortOrder));
    }

    [Fact]
    public async Task DeleteClubMarksClubInactiveAndKeepsShots()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var clubId = await scope.SeedClubAsync("7 Iron", ClubType.Iron, isActive: true, sortOrder: 4);
        var sessionId = await scope.SeedPracticeSessionAsync();
        await scope.SeedShotAsync(sessionId, clubId);

        await scope.Clubs.DeleteAsync(clubId, CancellationToken.None);

        await using var connection = scope.OpenConnection();
        await connection.OpenAsync(CancellationToken.None);

        var club = await scope.Clubs.GetAsync(clubId, CancellationToken.None);
        var activeResults = await scope.Clubs.SearchAsync(
            new ClubSearchCriteria(scope.DefaultGolferProfileId, ActiveOnly: true),
            CancellationToken.None);

        Assert.NotNull(club);
        Assert.False(club!.IsActive);
        Assert.DoesNotContain(activeResults, item => item.Id == clubId);
        Assert.Equal(1L, await scope.ScalarAsync<long>(connection, $"SELECT COUNT(*) FROM Shots WHERE ClubId = '{clubId}';"));
    }

    [Fact]
    public async Task SaveAsyncRejectsDuplicateActiveClubNamesWithinTheSameBag()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var first = new Club
        {
            Id = Guid.NewGuid(),
            GolferProfileId = scope.DefaultGolferProfileId,
            Name = "Practice Iron",
            ClubType = ClubType.Iron,
            IsActive = true,
            SortOrder = 42,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var duplicate = first with
        {
            Id = Guid.NewGuid(),
            SortOrder = 43,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await scope.Clubs.SaveAsync(first, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scope.Clubs.SaveAsync(duplicate, CancellationToken.None));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsyncAllowsReusingANameAfterTheOriginalClubIsInactivated()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var original = new Club
        {
            Id = Guid.NewGuid(),
            GolferProfileId = scope.DefaultGolferProfileId,
            Name = "Driving Wood",
            ClubType = ClubType.FairwayWood,
            IsActive = true,
            SortOrder = 20,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var replacement = original with
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            SortOrder = 21,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        await scope.Clubs.SaveAsync(original, CancellationToken.None);
        await scope.Clubs.DeleteAsync(original.Id, CancellationToken.None);
        await scope.Clubs.SaveAsync(replacement, CancellationToken.None);

        var activeClubs = await scope.Clubs.SearchAsync(
            new ClubSearchCriteria(scope.DefaultGolferProfileId, ActiveOnly: true),
            CancellationToken.None);

        Assert.Contains(activeClubs, item => item.Id == replacement.Id && item.Name == "Driving Wood");
        Assert.DoesNotContain(activeClubs, item => item.Id == original.Id);
    }
}
