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
            Name = "7 Iron",
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

        await scope.SeedClubAsync("Utility Iron", ClubType.UtilityIron, isActive: true, sortOrder: 0);
        await scope.SeedClubAsync("Old 3 Wood", ClubType.FairwayWood, isActive: false, sortOrder: 1);

        var results = await scope.Clubs.SearchAsync(
            new ClubSearchCriteria(scope.DefaultGolferProfileId, ActiveOnly: true, SearchText: "utility"),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("Utility Iron", results[0].Name);
        Assert.True(results[0].IsActive);
    }

    [Fact]
    public async Task DeleteClubRemovesAssociatedShots()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var clubId = await scope.SeedClubAsync("7 Iron", ClubType.Iron, isActive: true, sortOrder: 4);
        var sessionId = await scope.SeedPracticeSessionAsync();
        await scope.SeedShotAsync(sessionId, clubId);

        await scope.Clubs.DeleteAsync(clubId, CancellationToken.None);

        await using var connection = scope.OpenConnection();
        await connection.OpenAsync(CancellationToken.None);

        Assert.Equal(0L, await scope.ScalarAsync<long>(connection, $"SELECT COUNT(*) FROM Clubs WHERE Id = '{clubId}';"));
        Assert.Equal(0L, await scope.ScalarAsync<long>(connection, $"SELECT COUNT(*) FROM Shots WHERE ClubId = '{clubId}';"));
    }
}
