using System.Data.Common;
using System.Globalization;

namespace CarryIQ.IntegrationTests.Persistence;

public class DuckDbDatabaseInitializerTests
{
    [Fact]
    public async Task InitializeCreatesSchemaAndSeedsDefaultData()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        await using var connection = scope.OpenConnection();
        await connection.OpenAsync(CancellationToken.None);

        Assert.Equal(2L, await scope.ScalarAsync<long>(connection, "SELECT MAX(Version) FROM SchemaVersion;"));
        Assert.Equal(1L, await scope.ScalarAsync<long>(connection, "SELECT COUNT(*) FROM GolferProfiles;"));
        Assert.Equal(15L, await scope.ScalarAsync<long>(connection, "SELECT COUNT(*) FROM Clubs;"));
    }

    [Fact]
    public async Task InitializeIsIdempotent()
    {
        using var scope = new TestScope();

        await scope.Initializer.InitializeAsync(CancellationToken.None);
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        await using var connection = scope.OpenConnection();
        await connection.OpenAsync(CancellationToken.None);

        Assert.Equal(1L, await scope.ScalarAsync<long>(connection, "SELECT COUNT(*) FROM GolferProfiles;"));
        Assert.Equal(15L, await scope.ScalarAsync<long>(connection, "SELECT COUNT(*) FROM Clubs;"));
    }

    [Fact]
    public async Task InitializeAppliesPendingMigrationsWithoutBreakingSeedData()
    {
        using var scope = new TestScope();
        await scope.CreateVersion1DatabaseAsync();

        await scope.Initializer.InitializeAsync(CancellationToken.None);

        await using var connection = scope.OpenConnection();
        await connection.OpenAsync(CancellationToken.None);

        Assert.Equal(2L, await scope.ScalarAsync<long>(connection, "SELECT MAX(Version) FROM SchemaVersion;"));
        Assert.Equal(1L, await scope.ScalarAsync<long>(connection, "SELECT COUNT(*) FROM GolferProfiles;"));
        Assert.Equal(15L, await scope.ScalarAsync<long>(connection, "SELECT COUNT(*) FROM Clubs;"));
    }
}
