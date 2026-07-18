using System.Data.Common;
using System.Globalization;

namespace CarryIQ.Infrastructure;

public sealed class DuckDbDatabaseInitializer : IDatabaseInitializer
{
    private const int CurrentSchemaVersion = 1;

    private readonly IApplicationPaths _applicationPaths;
    private readonly IDatabaseConnectionFactory _connectionFactory;

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
        IDatabaseConnectionFactory connectionFactory)
    {
        _applicationPaths = applicationPaths;
        _connectionFactory = connectionFactory;
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
            await ExecuteNonQueryAsync(connection, transaction, SchemaSql, cancellationToken);
            await EnsureSchemaVersionAsync(connection, transaction, cancellationToken);
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

    private static async Task EnsureSchemaVersionAsync(
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = "SELECT COUNT(*) FROM SchemaVersion;";
        var count = Convert.ToInt32(await query.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);

        if (count > 0)
        {
            return;
        }

        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO SchemaVersion (Version, AppliedAtUtc)
            VALUES ($version, CURRENT_TIMESTAMP);
            """;
        AddParameter(insert, "$version", CurrentSchemaVersion);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Guid> EnsureDefaultGolferProfileAsync(
        DbConnection connection,
        DbTransaction transaction,
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
        DbConnection connection,
        DbTransaction transaction,
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

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS SchemaVersion (
            Version INTEGER NOT NULL,
            AppliedAtUtc TIMESTAMP NOT NULL
        );

        CREATE TABLE IF NOT EXISTS GolferProfiles (
            Id UUID PRIMARY KEY,
            DisplayName TEXT NOT NULL,
            HandicapIndex DECIMAL(6, 2) NULL,
            DominantHand INTEGER NOT NULL,
            DefaultDistanceUnit INTEGER NOT NULL,
            DefaultSpeedUnit INTEGER NOT NULL,
            DefaultTemperatureUnit INTEGER NOT NULL,
            CreatedAt TIMESTAMP NOT NULL,
            UpdatedAt TIMESTAMP NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Clubs (
            Id UUID PRIMARY KEY,
            GolferProfileId UUID NOT NULL,
            Name TEXT NOT NULL,
            ClubType INTEGER NOT NULL,
            Manufacturer TEXT NULL,
            Model TEXT NULL,
            Loft DECIMAL(6, 2) NULL,
            Shaft TEXT NULL,
            ShaftFlex TEXT NULL,
            LengthYards DOUBLE NULL,
            IsActive BOOLEAN NOT NULL,
            SortOrder INTEGER NOT NULL,
            Notes TEXT NULL,
            CreatedAt TIMESTAMP NOT NULL,
            UpdatedAt TIMESTAMP NOT NULL
        );

        CREATE TABLE IF NOT EXISTS PracticeSessions (
            Id UUID PRIMARY KEY,
            GolferProfileId UUID NOT NULL,
            Name TEXT NOT NULL,
            SessionDate DATE NOT NULL,
            StartTime TIME NULL,
            EndTime TIME NULL,
            LocationName TEXT NULL,
            SessionType INTEGER NOT NULL,
            SurfaceType INTEGER NOT NULL,
            BallType TEXT NULL,
            LaunchMonitorSource TEXT NULL,
            WeatherDescription TEXT NULL,
            TemperatureCelsius DOUBLE NULL,
            WindSpeedMilesPerHour DOUBLE NULL,
            WindDirection TEXT NULL,
            ElevationMetres DOUBLE NULL,
            Notes TEXT NULL,
            CreatedAt TIMESTAMP NOT NULL,
            UpdatedAt TIMESTAMP NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Shots (
            Id UUID PRIMARY KEY,
            PracticeSessionId UUID NOT NULL,
            ClubId UUID NOT NULL,
            ShotSequence INTEGER NOT NULL,
            RecordedAt TIMESTAMP NOT NULL,
            Source INTEGER NOT NULL,
            CarryDistanceYards DOUBLE NULL,
            TotalDistanceYards DOUBLE NULL,
            BallSpeedMilesPerHour DOUBLE NULL,
            ClubSpeedMilesPerHour DOUBLE NULL,
            SmashFactor DOUBLE NULL,
            LaunchAngle DOUBLE NULL,
            LaunchDirection DOUBLE NULL,
            ApexHeight DOUBLE NULL,
            SpinRate DOUBLE NULL,
            SpinAxis DOUBLE NULL,
            OfflineDistanceYards DOUBLE NULL,
            RollDistanceYards DOUBLE NULL,
            HangTime DOUBLE NULL,
            AttackAngle DOUBLE NULL,
            ClubPath DOUBLE NULL,
            FaceAngle DOUBLE NULL,
            FaceToPath DOUBLE NULL,
            DynamicLoft DOUBLE NULL,
            StrikeQuality INTEGER NULL,
            ShotShape INTEGER NULL,
            LieType TEXT NULL,
            SwingType INTEGER NULL,
            TargetDistanceYards DOUBLE NULL,
            IsIncluded BOOLEAN NOT NULL,
            ExclusionReason TEXT NULL,
            IsEstimated BOOLEAN NOT NULL,
            Notes TEXT NULL,
            RawImportData TEXT NULL,
            CreatedAt TIMESTAMP NOT NULL,
            UpdatedAt TIMESTAMP NOT NULL
        );

        CREATE TABLE IF NOT EXISTS WedgeSwingReferences (
            Id UUID PRIMARY KEY,
            GolferProfileId UUID NOT NULL,
            ClubId UUID NOT NULL,
            SwingLabel TEXT NOT NULL,
            SwingType INTEGER NOT NULL,
            ClockPosition TEXT NULL,
            TargetDistanceYards DOUBLE NULL,
            AverageCarryYards DOUBLE NULL,
            CarryStandardDeviationYards DOUBLE NULL,
            SampleSize INTEGER NOT NULL,
            IsManualOverride BOOLEAN NOT NULL,
            UpdatedAt TIMESTAMP NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ImportJobs (
            Id UUID PRIMARY KEY,
            FileName TEXT NOT NULL,
            Importer TEXT NOT NULL,
            StartedAtUtc TIMESTAMP NOT NULL,
            CompletedAtUtc TIMESTAMP NULL,
            Status TEXT NOT NULL,
            RowsRead INTEGER NOT NULL,
            RowsImported INTEGER NOT NULL,
            RowsSkipped INTEGER NOT NULL,
            RowsFailed INTEGER NOT NULL,
            PracticeSessionId UUID NULL,
            ErrorSummary TEXT NULL
        );

        CREATE TABLE IF NOT EXISTS ImportErrors (
            Id UUID PRIMARY KEY,
            ImportJobId UUID NOT NULL,
            RowNumber INTEGER NOT NULL,
            FieldName TEXT NOT NULL,
            Message TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS SavedMappings (
            Id UUID PRIMARY KEY,
            Name TEXT NOT NULL,
            Importer TEXT NOT NULL,
            MappingJson TEXT NOT NULL,
            CreatedAtUtc TIMESTAMP NOT NULL,
            UpdatedAtUtc TIMESTAMP NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ApplicationSettings (
            Id UUID PRIMARY KEY,
            SettingsJson TEXT NOT NULL,
            SchemaVersion INTEGER NOT NULL,
            UpdatedAtUtc TIMESTAMP NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Backups (
            Id UUID PRIMARY KEY,
            BackupPath TEXT NOT NULL,
            CreatedAtUtc TIMESTAMP NOT NULL,
            Notes TEXT NULL
        );
        """;
}
