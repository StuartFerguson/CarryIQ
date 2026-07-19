using System.Data.Common;
using System.Globalization;
using System.Text;

namespace CarryIQ.Infrastructure;

public sealed class DuckDbMigrationRunner
{
    private readonly string _migrationDirectory = Path.Combine(AppContext.BaseDirectory, "Migrations");

    public async Task ApplyPendingMigrationsAsync(
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var currentVersion = await GetCurrentVersionAsync(connection, transaction, cancellationToken);

        if (!Directory.Exists(_migrationDirectory))
        {
            throw new DirectoryNotFoundException($"Migration directory was not found: {_migrationDirectory}");
        }

        foreach (var migrationPath in Directory.EnumerateFiles(_migrationDirectory, "*.sql")
                     .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            var migrationVersion = ParseMigrationVersion(Path.GetFileName(migrationPath));
            if (migrationVersion <= currentVersion)
            {
                continue;
            }

            var sql = await File.ReadAllTextAsync(migrationPath, Encoding.UTF8, cancellationToken);
            await ExecuteNonQueryAsync(connection, transaction, sql, cancellationToken);
            currentVersion = migrationVersion;
        }
    }

    private static int ParseMigrationVersion(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Migration file name is missing.");
        }

        var underscoreIndex = fileName.IndexOf('_');
        var versionText = underscoreIndex > 0 ? fileName[..underscoreIndex] : Path.GetFileNameWithoutExtension(fileName);

        if (!int.TryParse(versionText, NumberStyles.None, CultureInfo.InvariantCulture, out var version))
        {
            throw new InvalidOperationException($"Migration file name does not start with a numeric version: {fileName}");
        }

        return version;
    }

    private static async Task<int> GetCurrentVersionAsync(
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion;";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is null || result is DBNull ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
        }
        catch (DbException)
        {
            return 0;
        }
    }

    private static async Task ExecuteNonQueryAsync(
        DbConnection connection,
        DbTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
