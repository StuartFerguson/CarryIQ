using CarryIQ.IntegrationTests.Persistence;
using CarryIQ.Infrastructure;

namespace CarryIQ.IntegrationTests.Utilities;

public class DemoDataSeederTests
{
    [Fact]
    public async Task SeedAsyncCreatesStarterClubsWhenBagIsEmptyAndAddsTheRequestedSessions()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        await ClearClubsAsync(scope);

        var seeder = new DemoDataSeeder(
            scope.Clubs,
            scope.Sessions,
            scope.Shots,
            new DuckDbConnectionFactory(scope.Paths));

        var result = await seeder.SeedAsync(new DemoDataSeedOptions(20, 4), CancellationToken.None);

        Assert.Equal(8, result.CreatedClubCount);
        Assert.Equal(8, result.AvailableClubCount);
        Assert.Equal(20, result.SessionCount);
        Assert.True(result.ShotCount >= 160);

        var clubs = await scope.Clubs.SearchAsync(new ClubSearchCriteria(ActiveOnly: true), CancellationToken.None);
        var sessions = await scope.Sessions.SearchAsync(new SessionSearchCriteria(), CancellationToken.None);
        var shots = await scope.Shots.SearchAsync(new ShotSearchCriteria(), CancellationToken.None);

        Assert.Equal(8, clubs.Count);
        Assert.Equal(20, sessions.Count);
        Assert.True(shots.Count >= 160);
    }

    [Fact]
    public async Task SeedAsyncReusesExistingClubsWhenTheBagAlreadyHasEntries()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var existingClubs = await scope.Clubs.SearchAsync(new ClubSearchCriteria(ActiveOnly: true), CancellationToken.None);
        var seeder = new DemoDataSeeder(
            scope.Clubs,
            scope.Sessions,
            scope.Shots,
            new DuckDbConnectionFactory(scope.Paths));

        var result = await seeder.SeedAsync(new DemoDataSeedOptions(3, 2), CancellationToken.None);

        Assert.Equal(0, result.CreatedClubCount);
        Assert.Equal(existingClubs.Count, result.AvailableClubCount);
    }

    private static async Task ClearClubsAsync(TestScope scope)
    {
        await using var connection = scope.OpenConnection();
        await connection.OpenAsync(CancellationToken.None);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Clubs;";
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }
}
