using System.Data.Common;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace CarryIQ.IntegrationTests.Persistence;

public sealed class TestScope : IDisposable
{
    private readonly string _rootDirectory;
    private readonly TestApplicationPaths _paths;
    private readonly DuckDbConnectionFactory _connectionFactory;

    public TestScope()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), "CarryIQ", Guid.NewGuid().ToString("N"));
        _paths = new TestApplicationPaths(_rootDirectory);
        _connectionFactory = new DuckDbConnectionFactory(_paths);
        Initializer = new DuckDbDatabaseInitializer(_paths, _connectionFactory, new DuckDbMigrationRunner());
        Clubs = new DuckDbClubRepository(_connectionFactory);
        Sessions = new DuckDbPracticeSessionRepository(_connectionFactory);
        Shots = new DuckDbShotRepository(_connectionFactory);
    }

    public DuckDbDatabaseInitializer Initializer { get; }

    public IClubRepository Clubs { get; }

    public IPracticeSessionRepository Sessions { get; }

    public IShotRepository Shots { get; }

    public IApplicationPaths Paths => _paths;

    public DbConnection OpenConnection() => _connectionFactory.CreateConnection();

    public async Task<T> ScalarAsync<T>(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(CancellationToken.None);
        if (result is BigInteger bigInteger && typeof(T) == typeof(long))
        {
            return (T)(object)(long)bigInteger;
        }

        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture)!;
    }

    public async Task CreateVersion1DatabaseAsync()
    {
        await ExecuteSqlFileAsync("001_initial.sql");
    }

    public async Task<Guid> SeedClubAsync(string name, ClubType clubType, bool isActive, int sortOrder)
    {
        var id = Guid.NewGuid();
        await ExecuteNonQueryAsync("""
            INSERT INTO Clubs (
                Id, GolferProfileId, Name, ClubType, Manufacturer, Model, Loft,
                Shaft, ShaftFlex, LengthYards, IsActive, SortOrder, Notes, CreatedAt, UpdatedAt)
            VALUES (
                $id, $golferProfileId, $name, $clubType, NULL, NULL, NULL,
                NULL, NULL, NULL, $isActive, $sortOrder, NULL, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
            """,
            new Dictionary<string, object?>
            {
                ["$id"] = id,
                ["$golferProfileId"] = DefaultGolferProfileId,
                ["$name"] = name,
                ["$clubType"] = (int)clubType,
                ["$isActive"] = isActive,
                ["$sortOrder"] = sortOrder,
            });

        return id;
    }

    public async Task<Guid> SeedPracticeSessionAsync(bool isArchived = false)
    {
        var id = Guid.NewGuid();
        await ExecuteNonQueryAsync("""
            INSERT INTO PracticeSessions (
                Id, GolferProfileId, Name, SessionDate, StartTime, EndTime, LocationName,
                SessionType, SurfaceType, BallType, LaunchMonitorSource, WeatherDescription,
                TemperatureCelsius, WindSpeedMilesPerHour, WindDirection, ElevationMetres, Notes,
                IsArchived, CreatedAt, UpdatedAt)
            VALUES (
                $id, $golferProfileId, $name, $sessionDate, NULL, NULL, $locationName,
                $sessionType, $surfaceType, $ballType, $launchMonitorSource, NULL,
                NULL, NULL, NULL, NULL, $notes, $isArchived, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
            """,
            new Dictionary<string, object?>
            {
                ["$id"] = id,
                ["$golferProfileId"] = DefaultGolferProfileId,
                ["$name"] = "Morning Range",
                ["$sessionDate"] = new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc),
                ["$locationName"] = "South Range",
                ["$sessionType"] = (int)SessionType.DrivingRange,
                ["$surfaceType"] = (int)SurfaceType.Grass,
                ["$ballType"] = "Titleist Pro V1",
                ["$launchMonitorSource"] = "Trackman",
                ["$notes"] = "Baseline session",
                ["$isArchived"] = isArchived,
            });

        return id;
    }

    public async Task<Shot> SeedShotAsync(Guid practiceSessionId, Guid clubId, bool isIncluded = true)
    {
        var shot = new Shot
        {
            Id = Guid.NewGuid(),
            PracticeSessionId = practiceSessionId,
            ClubId = clubId,
            ShotSequence = 1,
            RecordedAt = DateTimeOffset.Parse("2026-07-19T10:00:00Z"),
            Source = ShotSourceKind.Manual,
            CarryDistance = Distance.FromYards(150m),
            TotalDistance = Distance.FromYards(158m),
            BallSpeed = Speed.FromMilesPerHour(111m),
            ClubSpeed = Speed.FromMilesPerHour(83m),
            SmashFactor = 1.34m,
            LaunchAngle = 14.2m,
            LaunchDirection = -1.5m,
            ApexHeight = 28m,
            SpinRate = 6200m,
            SpinAxis = -2m,
            OfflineDistance = Distance.FromYards(4m),
            RollDistance = Distance.FromYards(8m),
            HangTime = 4.8m,
            AttackAngle = -4.2m,
            ClubPath = 1.2m,
            FaceAngle = 0.4m,
            FaceToPath = -0.8m,
            DynamicLoft = 23m,
            StrikeQuality = StrikeQuality.Good,
            ShotShape = ShotShape.Draw,
            LieType = "Tee",
            SwingType = SwingType.Full,
            TargetDistance = Distance.FromYards(155m),
            IsIncluded = isIncluded,
            ExclusionReason = isIncluded ? null : "Example exclusion",
            IsEstimated = false,
            Notes = "Seed shot",
            RawImportData = null,
            CreatedAt = DateTimeOffset.Parse("2026-07-19T10:00:00Z"),
            UpdatedAt = DateTimeOffset.Parse("2026-07-19T10:00:00Z"),
        };

        await ExecuteNonQueryAsync("""
            INSERT INTO Shots (
                Id, PracticeSessionId, ClubId, ShotSequence, RecordedAt, Source,
                CarryDistanceYards, TotalDistanceYards, BallSpeedMilesPerHour, ClubSpeedMilesPerHour,
                SmashFactor, LaunchAngle, LaunchDirection, ApexHeight, SpinRate, SpinAxis,
                OfflineDistanceYards, RollDistanceYards, HangTime, AttackAngle, ClubPath, FaceAngle,
                FaceToPath, DynamicLoft, StrikeQuality, ShotShape, LieType, SwingType,
                TargetDistanceYards, IsIncluded, ExclusionReason, IsEstimated, Notes, RawImportData,
                CreatedAt, UpdatedAt)
            VALUES (
                $id, $practiceSessionId, $clubId, $shotSequence, $recordedAt, $source,
                $carryDistanceYards, $totalDistanceYards, $ballSpeedMilesPerHour, $clubSpeedMilesPerHour,
                $smashFactor, $launchAngle, $launchDirection, $apexHeight, $spinRate, $spinAxis,
                $offlineDistanceYards, $rollDistanceYards, $hangTime, $attackAngle, $clubPath, $faceAngle,
                $faceToPath, $dynamicLoft, $strikeQuality, $shotShape, $lieType, $swingType,
                $targetDistanceYards, $isIncluded, $exclusionReason, $isEstimated, $notes, $rawImportData,
                $createdAt, $updatedAt);
            """,
            new Dictionary<string, object?>
            {
                ["$id"] = shot.Id,
                ["$practiceSessionId"] = shot.PracticeSessionId,
                ["$clubId"] = shot.ClubId,
                ["$shotSequence"] = shot.ShotSequence,
                ["$recordedAt"] = DuckDbPersistenceHelpers.ToDbValue(shot.RecordedAt),
                ["$source"] = (int)shot.Source,
                ["$carryDistanceYards"] = DuckDbPersistenceHelpers.ToDbValue(shot.CarryDistance),
                ["$totalDistanceYards"] = DuckDbPersistenceHelpers.ToDbValue(shot.TotalDistance),
                ["$ballSpeedMilesPerHour"] = DuckDbPersistenceHelpers.ToDbValue(shot.BallSpeed),
                ["$clubSpeedMilesPerHour"] = DuckDbPersistenceHelpers.ToDbValue(shot.ClubSpeed),
                ["$smashFactor"] = shot.SmashFactor,
                ["$launchAngle"] = shot.LaunchAngle,
                ["$launchDirection"] = shot.LaunchDirection,
                ["$apexHeight"] = shot.ApexHeight,
                ["$spinRate"] = shot.SpinRate,
                ["$spinAxis"] = shot.SpinAxis,
                ["$offlineDistanceYards"] = DuckDbPersistenceHelpers.ToDbValue(shot.OfflineDistance),
                ["$rollDistanceYards"] = DuckDbPersistenceHelpers.ToDbValue(shot.RollDistance),
                ["$hangTime"] = shot.HangTime,
                ["$attackAngle"] = shot.AttackAngle,
                ["$clubPath"] = shot.ClubPath,
                ["$faceAngle"] = shot.FaceAngle,
                ["$faceToPath"] = shot.FaceToPath,
                ["$dynamicLoft"] = shot.DynamicLoft,
                ["$strikeQuality"] = shot.StrikeQuality is null ? null : (int)shot.StrikeQuality.Value,
                ["$shotShape"] = shot.ShotShape is null ? null : (int)shot.ShotShape.Value,
                ["$lieType"] = shot.LieType,
                ["$swingType"] = shot.SwingType is null ? null : (int)shot.SwingType.Value,
                ["$targetDistanceYards"] = DuckDbPersistenceHelpers.ToDbValue(shot.TargetDistance),
                ["$isIncluded"] = shot.IsIncluded,
                ["$exclusionReason"] = shot.ExclusionReason,
                ["$isEstimated"] = shot.IsEstimated,
                ["$notes"] = shot.Notes,
                ["$rawImportData"] = shot.RawImportData,
                ["$createdAt"] = DuckDbPersistenceHelpers.ToDbValue(shot.CreatedAt),
                ["$updatedAt"] = DuckDbPersistenceHelpers.ToDbValue(shot.UpdatedAt),
            });

        return shot;
    }

    public Guid DefaultGolferProfileId => GetDefaultGolferProfileIdAsync().GetAwaiter().GetResult();

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }

    private async Task ExecuteSqlFileAsync(string fileName)
    {
        Directory.CreateDirectory(_paths.DataDirectory);
        await using var connection = OpenConnection();
        await connection.OpenAsync(CancellationToken.None);

        await using var transaction = await connection.BeginTransactionAsync(CancellationToken.None);
        try
        {
            var sql = await LoadMigrationSqlAsync(fileName);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task ExecuteNonQueryAsync(string sql, IReadOnlyDictionary<string, object?> parameters)
    {
        Directory.CreateDirectory(_paths.DataDirectory);
        await using var connection = OpenConnection();
        await connection.OpenAsync(CancellationToken.None);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            DuckDbPersistenceHelpers.AddParameter(command, parameter.Key, parameter.Value);
        }

        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private async Task<Guid> GetDefaultGolferProfileIdAsync()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM GolferProfiles ORDER BY CreatedAt LIMIT 1;";
        var result = await command.ExecuteScalarAsync(CancellationToken.None);
        return result switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text, CultureInfo.InvariantCulture),
            _ => Guid.Parse(Convert.ToString(result, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture),
        };
    }

    private static async Task<string> LoadMigrationSqlAsync(string fileName)
    {
        var repoRoot = FindRepositoryRoot();
        var sourcePath = Path.Combine(repoRoot, "src", "CarryIQ.Infrastructure", "Migrations", fileName);
        if (File.Exists(sourcePath))
        {
            return await File.ReadAllTextAsync(sourcePath, Encoding.UTF8, CancellationToken.None);
        }

        var outputPath = Path.Combine(AppContext.BaseDirectory, "Migrations", fileName);
        return await File.ReadAllTextAsync(outputPath, Encoding.UTF8, CancellationToken.None);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CarryIQ.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
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
