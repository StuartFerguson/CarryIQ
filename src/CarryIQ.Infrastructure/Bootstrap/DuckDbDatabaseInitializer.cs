using System.Data.Common;
using System.Globalization;

namespace CarryIQ.Infrastructure;

public sealed class DuckDbDatabaseInitializer : IDatabaseInitializer
{
    private readonly IApplicationPaths _applicationPaths;
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly DuckDbMigrationRunner _migrationRunner;

    private static readonly string[] StarterClubs =
    [
        "Driver",
        "3 Wood",
        "5 Wood",
        "3 Iron",
        "4 Iron",
        "5 Iron",
        "6 Iron",
        "7 Iron",
        "8 Iron",
        "9 Iron",
        "Pitching Wedge",
        "Gap Wedge",
        "Sand Wedge",
        "Lob Wedge",
        "Putter",
    ];

    public DuckDbDatabaseInitializer(
        IApplicationPaths applicationPaths,
        IDatabaseConnectionFactory connectionFactory,
        DuckDbMigrationRunner migrationRunner)
    {
        _applicationPaths = applicationPaths;
        _connectionFactory = connectionFactory;
        _migrationRunner = migrationRunner;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_applicationPaths.DataDirectory);
        Directory.CreateDirectory(_applicationPaths.LogsDirectory);
        Directory.CreateDirectory(_applicationPaths.BackupsDirectory);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await _migrationRunner.ApplyPendingMigrationsAsync(connection, transaction, cancellationToken);
            var golferProfileId = await EnsureDefaultGolferProfileAsync(connection, transaction, cancellationToken);
            await EnsureStarterBagAsync(connection, transaction, golferProfileId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<Guid> EnsureDefaultGolferProfileAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = "SELECT Id FROM GolferProfiles ORDER BY CreatedAt LIMIT 1;";
        var existing = await query.ExecuteScalarAsync(cancellationToken);
        if (existing is not null)
        {
            return existing is Guid existingId
                ? existingId
                : Guid.Parse(existing.ToString()!);
        }

        var profileId = Guid.NewGuid();
        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO GolferProfiles (
                Id, DisplayName, HandicapIndex, DominantHand, DefaultDistanceUnit,
                DefaultSpeedUnit, DefaultTemperatureUnit, CreatedAt, UpdatedAt)
            VALUES (
                $id, $displayName, $handicapIndex, $dominantHand, $distanceUnit,
                $speedUnit, $temperatureUnit, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
            """;
        AddParameter(insert, "$id", profileId);
        AddParameter(insert, "$displayName", "Default Golfer");
        AddParameter(insert, "$handicapIndex", DBNull.Value);
        AddParameter(insert, "$dominantHand", (int)DominantHand.Right);
        AddParameter(insert, "$distanceUnit", (int)DistanceUnit.Yards);
        AddParameter(insert, "$speedUnit", (int)SpeedUnit.MilesPerHour);
        AddParameter(insert, "$temperatureUnit", (int)TemperatureUnit.Celsius);
        await insert.ExecuteNonQueryAsync(cancellationToken);

        return profileId;
    }

    private static async Task EnsureStarterBagAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction transaction,
        Guid golferProfileId,
        CancellationToken cancellationToken)
    {
        await using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = "SELECT COUNT(*) FROM Clubs WHERE GolferProfileId = $golferProfileId;";
        AddParameter(query, "$golferProfileId", golferProfileId);
        var count = Convert.ToInt32(await query.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (count > 0)
        {
            return;
        }

        for (var index = 0; index < StarterClubs.Length; index++)
        {
            var clubName = StarterClubs[index];
            await using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO Clubs (
                    Id, GolferProfileId, Name, ClubType, Manufacturer, Model, Loft,
                    Shaft, ShaftFlex, LengthYards, IsActive, SortOrder, Notes, CreatedAt, UpdatedAt)
                VALUES (
                    $id, $golferProfileId, $name, $clubType, NULL, NULL, NULL,
                    NULL, NULL, NULL, TRUE, $sortOrder, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
                """;
            AddParameter(insert, "$id", Guid.NewGuid());
            AddParameter(insert, "$golferProfileId", golferProfileId);
            AddParameter(insert, "$name", clubName);
            AddParameter(insert, "$clubType", (int)MapClubType(clubName));
            AddParameter(insert, "$sortOrder", index);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name.TrimStart('$', '@', ':');
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static ClubType MapClubType(string clubName) => clubName switch
    {
        "Driver" => ClubType.Driver,
        "3 Wood" or "5 Wood" => ClubType.FairwayWood,
        "3 Iron" or "4 Iron" or "5 Iron" or "6 Iron" or "7 Iron" or "8 Iron" or "9 Iron" => ClubType.Iron,
        "Pitching Wedge" => ClubType.PitchingWedge,
        "Gap Wedge" => ClubType.GapWedge,
        "Sand Wedge" => ClubType.SandWedge,
        "Lob Wedge" => ClubType.LobWedge,
        "Putter" => ClubType.Putter,
        _ => ClubType.Other,
    };
}
