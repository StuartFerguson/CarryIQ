using System.Data.Common;
using System.Globalization;
using DuckDB.NET.Data;

namespace CarryIQ.IntegrationTests.Persistence;

public class DuckDbDatabaseInitializerTests
{
    [Fact]
    public async Task InitializeCreatesSchemaAndSeedsDefaultData()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        await using var connection = new DuckDBConnection($"Data Source={scope.Paths.DatabasePath}");
        await connection.OpenAsync(CancellationToken.None);

        Assert.Equal(1L, await ScalarAsync<long>(connection, "SELECT COUNT(*) FROM SchemaVersion;"));
        Assert.Equal(1L, await ScalarAsync<long>(connection, "SELECT COUNT(*) FROM GolferProfiles;"));
        Assert.Equal(15L, await ScalarAsync<long>(connection, "SELECT COUNT(*) FROM Clubs;"));
    }

    [Fact]
    public async Task InitializeIsIdempotent()
    {
        using var scope = new TestScope();

        await scope.Initializer.InitializeAsync(CancellationToken.None);
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        await using var connection = new DuckDBConnection($"Data Source={scope.Paths.DatabasePath}");
        await connection.OpenAsync(CancellationToken.None);

        Assert.Equal(1L, await ScalarAsync<long>(connection, "SELECT COUNT(*) FROM GolferProfiles;"));
        Assert.Equal(15L, await ScalarAsync<long>(connection, "SELECT COUNT(*) FROM Clubs;"));
    }

    private static async Task<T> ScalarAsync<T>(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)Convert.ChangeType(await command.ExecuteScalarAsync(CancellationToken.None), typeof(T), CultureInfo.InvariantCulture)!;
    }

    private sealed class TestScope : IDisposable
    {
        public TestScope()
        {
            RootDirectory = Path.Combine(Path.GetTempPath(), "CarryIQ", Guid.NewGuid().ToString("N"));
            Paths = new TestApplicationPaths(RootDirectory);
            Initializer = new DuckDbDatabaseInitializer(Paths, new DuckDbConnectionFactory(Paths));
        }

        public string RootDirectory { get; }

        public TestApplicationPaths Paths { get; }

        public DuckDbDatabaseInitializer Initializer { get; }

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }

    private sealed class TestApplicationPaths(string rootDirectory) : IApplicationPaths
    {
        public string DataDirectory => rootDirectory;

        public string DatabasePath => Path.Combine(rootDirectory, "carryiq.duckdb");

        public string SettingsPath => Path.Combine(rootDirectory, "user-settings.json");

        public string LogsDirectory => Path.Combine(rootDirectory, "logs");

        public string BackupsDirectory => Path.Combine(rootDirectory, "backups");
    }
}
