using System.Data.Common;
using System.Globalization;
using System.Text;

namespace CarryIQ.Infrastructure;

public sealed class DuckDbShotRepository : IShotRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public DuckDbShotRepository(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task AddAsync(Shot shot, CancellationToken cancellationToken) =>
        ExecuteWithTransactionAsync((connection, transaction, token) => UpsertAsync(connection, transaction, shot, token), cancellationToken);

    public async Task AddRangeAsync(IReadOnlyCollection<Shot> shots, CancellationToken cancellationToken)
    {
        if (shots.Count == 0)
        {
            return;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var shot in shots)
            {
                await UpsertAsync(connection, transaction, shot, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public Task UpdateAsync(Shot shot, CancellationToken cancellationToken) =>
        ExecuteWithTransactionAsync((connection, transaction, token) => UpsertAsync(connection, transaction, shot, token), cancellationToken);

    public async Task<IReadOnlyList<Shot>> SearchAsync(ShotSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var sql = new StringBuilder("""
            SELECT Id, PracticeSessionId, ClubId, ShotSequence, RecordedAt, Source,
                   CarryDistanceYards, TotalDistanceYards, BallSpeedMilesPerHour, ClubSpeedMilesPerHour,
                   SmashFactor, LaunchAngle, LaunchDirection, ApexHeight, SpinRate, SpinAxis,
                   OfflineDistanceYards, RollDistanceYards, HangTime, AttackAngle, ClubPath, FaceAngle,
                   FaceToPath, DynamicLoft, StrikeQuality, ShotShape, LieType, SwingType,
                   TargetDistanceYards, IsIncluded, ExclusionReason, IsEstimated, Notes, RawImportData,
                   CreatedAt, UpdatedAt
            FROM Shots
            WHERE 1 = 1
            """);

        var searchPattern = BuildSearchPattern(criteria.SearchText);
        if (criteria.PracticeSessionId is Guid)
        {
            sql.AppendLine("AND PracticeSessionId = $practiceSessionId");
        }

        if (criteria.ClubId is Guid)
        {
            sql.AppendLine("AND ClubId = $clubId");
        }

        if (criteria.StartDate is DateOnly)
        {
            sql.AppendLine("AND RecordedAt >= $startDate");
        }

        if (criteria.EndDate is DateOnly)
        {
            sql.AppendLine("AND RecordedAt < $endDateExclusive");
        }

        if (criteria.IncludedOnly is bool)
        {
            sql.AppendLine("AND IsIncluded = $includedOnly");
        }

        if (searchPattern is not null)
        {
            sql.AppendLine("""
                AND (
                    LOWER(COALESCE(Notes, '')) LIKE $searchPattern
                    OR LOWER(COALESCE(ExclusionReason, '')) LIKE $searchPattern
                    OR LOWER(COALESCE(LieType, '')) LIKE $searchPattern
                    OR LOWER(COALESCE(RawImportData, '')) LIKE $searchPattern
                )
                """);
        }

        sql.AppendLine("ORDER BY RecordedAt, ShotSequence;");

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();

        if (criteria.PracticeSessionId is Guid practiceSessionId)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$practiceSessionId", practiceSessionId);
        }

        if (criteria.ClubId is Guid clubId)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$clubId", clubId);
        }

        if (criteria.StartDate is DateOnly startDate)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$startDate", DuckDbPersistenceHelpers.ToDbValue(startDate));
        }

        if (criteria.EndDate is DateOnly endDate)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$endDateExclusive", DuckDbPersistenceHelpers.ToDbValue(endDate.AddDays(1)));
        }

        if (criteria.IncludedOnly is bool includedOnly)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$includedOnly", includedOnly);
        }

        if (searchPattern is not null)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$searchPattern", searchPattern);
        }

        var results = new List<Shot>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapShot(reader));
        }

        return results;
    }

    private Task ExecuteWithTransactionAsync(
        Func<DbConnection, DbTransaction, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        return ExecuteWithTransactionAsyncCore(action, cancellationToken);
    }

    private async Task ExecuteWithTransactionAsyncCore(
        Func<DbConnection, DbTransaction, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await action(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task UpsertAsync(
        DbConnection connection,
        DbTransaction transaction,
        Shot shot,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
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
                $createdAt, $updatedAt)
            ON CONFLICT (Id) DO UPDATE SET
                PracticeSessionId = excluded.PracticeSessionId,
                ClubId = excluded.ClubId,
                ShotSequence = excluded.ShotSequence,
                RecordedAt = excluded.RecordedAt,
                Source = excluded.Source,
                CarryDistanceYards = excluded.CarryDistanceYards,
                TotalDistanceYards = excluded.TotalDistanceYards,
                BallSpeedMilesPerHour = excluded.BallSpeedMilesPerHour,
                ClubSpeedMilesPerHour = excluded.ClubSpeedMilesPerHour,
                SmashFactor = excluded.SmashFactor,
                LaunchAngle = excluded.LaunchAngle,
                LaunchDirection = excluded.LaunchDirection,
                ApexHeight = excluded.ApexHeight,
                SpinRate = excluded.SpinRate,
                SpinAxis = excluded.SpinAxis,
                OfflineDistanceYards = excluded.OfflineDistanceYards,
                RollDistanceYards = excluded.RollDistanceYards,
                HangTime = excluded.HangTime,
                AttackAngle = excluded.AttackAngle,
                ClubPath = excluded.ClubPath,
                FaceAngle = excluded.FaceAngle,
                FaceToPath = excluded.FaceToPath,
                DynamicLoft = excluded.DynamicLoft,
                StrikeQuality = excluded.StrikeQuality,
                ShotShape = excluded.ShotShape,
                LieType = excluded.LieType,
                SwingType = excluded.SwingType,
                TargetDistanceYards = excluded.TargetDistanceYards,
                IsIncluded = excluded.IsIncluded,
                ExclusionReason = excluded.ExclusionReason,
                IsEstimated = excluded.IsEstimated,
                Notes = excluded.Notes,
                RawImportData = excluded.RawImportData,
                CreatedAt = excluded.CreatedAt,
                UpdatedAt = excluded.UpdatedAt;
            """;

        DuckDbPersistenceHelpers.AddParameter(command, "$id", shot.Id);
        DuckDbPersistenceHelpers.AddParameter(command, "$practiceSessionId", shot.PracticeSessionId);
        DuckDbPersistenceHelpers.AddParameter(command, "$clubId", shot.ClubId);
        DuckDbPersistenceHelpers.AddParameter(command, "$shotSequence", shot.ShotSequence);
        DuckDbPersistenceHelpers.AddParameter(command, "$recordedAt", DuckDbPersistenceHelpers.ToDbValue(shot.RecordedAt));
        DuckDbPersistenceHelpers.AddParameter(command, "$source", (int)shot.Source);
        DuckDbPersistenceHelpers.AddParameter(command, "$carryDistanceYards", DuckDbPersistenceHelpers.ToDbValue(shot.CarryDistance));
        DuckDbPersistenceHelpers.AddParameter(command, "$totalDistanceYards", DuckDbPersistenceHelpers.ToDbValue(shot.TotalDistance));
        DuckDbPersistenceHelpers.AddParameter(command, "$ballSpeedMilesPerHour", DuckDbPersistenceHelpers.ToDbValue(shot.BallSpeed));
        DuckDbPersistenceHelpers.AddParameter(command, "$clubSpeedMilesPerHour", DuckDbPersistenceHelpers.ToDbValue(shot.ClubSpeed));
        DuckDbPersistenceHelpers.AddParameter(command, "$smashFactor", shot.SmashFactor);
        DuckDbPersistenceHelpers.AddParameter(command, "$launchAngle", shot.LaunchAngle);
        DuckDbPersistenceHelpers.AddParameter(command, "$launchDirection", shot.LaunchDirection);
        DuckDbPersistenceHelpers.AddParameter(command, "$apexHeight", shot.ApexHeight);
        DuckDbPersistenceHelpers.AddParameter(command, "$spinRate", shot.SpinRate);
        DuckDbPersistenceHelpers.AddParameter(command, "$spinAxis", shot.SpinAxis);
        DuckDbPersistenceHelpers.AddParameter(command, "$offlineDistanceYards", DuckDbPersistenceHelpers.ToDbValue(shot.OfflineDistance));
        DuckDbPersistenceHelpers.AddParameter(command, "$rollDistanceYards", DuckDbPersistenceHelpers.ToDbValue(shot.RollDistance));
        DuckDbPersistenceHelpers.AddParameter(command, "$hangTime", shot.HangTime);
        DuckDbPersistenceHelpers.AddParameter(command, "$attackAngle", shot.AttackAngle);
        DuckDbPersistenceHelpers.AddParameter(command, "$clubPath", shot.ClubPath);
        DuckDbPersistenceHelpers.AddParameter(command, "$faceAngle", shot.FaceAngle);
        DuckDbPersistenceHelpers.AddParameter(command, "$faceToPath", shot.FaceToPath);
        DuckDbPersistenceHelpers.AddParameter(command, "$dynamicLoft", shot.DynamicLoft);
        DuckDbPersistenceHelpers.AddParameter(command, "$strikeQuality", shot.StrikeQuality is null ? null : (int)shot.StrikeQuality.Value);
        DuckDbPersistenceHelpers.AddParameter(command, "$shotShape", shot.ShotShape is null ? null : (int)shot.ShotShape.Value);
        DuckDbPersistenceHelpers.AddParameter(command, "$lieType", shot.LieType);
        DuckDbPersistenceHelpers.AddParameter(command, "$swingType", shot.SwingType is null ? null : (int)shot.SwingType.Value);
        DuckDbPersistenceHelpers.AddParameter(command, "$targetDistanceYards", DuckDbPersistenceHelpers.ToDbValue(shot.TargetDistance));
        DuckDbPersistenceHelpers.AddParameter(command, "$isIncluded", shot.IsIncluded);
        DuckDbPersistenceHelpers.AddParameter(command, "$exclusionReason", shot.ExclusionReason);
        DuckDbPersistenceHelpers.AddParameter(command, "$isEstimated", shot.IsEstimated);
        DuckDbPersistenceHelpers.AddParameter(command, "$notes", shot.Notes);
        DuckDbPersistenceHelpers.AddParameter(command, "$rawImportData", shot.RawImportData);
        DuckDbPersistenceHelpers.AddParameter(command, "$createdAt", DuckDbPersistenceHelpers.ToDbValue(shot.CreatedAt));
        DuckDbPersistenceHelpers.AddParameter(command, "$updatedAt", DuckDbPersistenceHelpers.ToDbValue(shot.UpdatedAt));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Shot MapShot(DbDataReader reader) =>
        new()
        {
            Id = DuckDbPersistenceHelpers.ReadGuid(reader, "Id"),
            PracticeSessionId = DuckDbPersistenceHelpers.ReadGuid(reader, "PracticeSessionId"),
            ClubId = DuckDbPersistenceHelpers.ReadGuid(reader, "ClubId"),
            ShotSequence = DuckDbPersistenceHelpers.ReadInt32(reader, "ShotSequence"),
            RecordedAt = DuckDbPersistenceHelpers.ReadDateTimeOffset(reader, "RecordedAt"),
            Source = DuckDbPersistenceHelpers.ReadEnum<ShotSourceKind>(reader, "Source"),
            CarryDistance = DuckDbPersistenceHelpers.ReadNullableDistance(reader, "CarryDistanceYards"),
            TotalDistance = DuckDbPersistenceHelpers.ReadNullableDistance(reader, "TotalDistanceYards"),
            BallSpeed = DuckDbPersistenceHelpers.ReadNullableSpeed(reader, "BallSpeedMilesPerHour"),
            ClubSpeed = DuckDbPersistenceHelpers.ReadNullableSpeed(reader, "ClubSpeedMilesPerHour"),
            SmashFactor = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "SmashFactor"),
            LaunchAngle = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "LaunchAngle"),
            LaunchDirection = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "LaunchDirection"),
            ApexHeight = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "ApexHeight"),
            SpinRate = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "SpinRate"),
            SpinAxis = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "SpinAxis"),
            OfflineDistance = DuckDbPersistenceHelpers.ReadNullableDistance(reader, "OfflineDistanceYards"),
            RollDistance = DuckDbPersistenceHelpers.ReadNullableDistance(reader, "RollDistanceYards"),
            HangTime = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "HangTime"),
            AttackAngle = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "AttackAngle"),
            ClubPath = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "ClubPath"),
            FaceAngle = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "FaceAngle"),
            FaceToPath = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "FaceToPath"),
            DynamicLoft = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "DynamicLoft"),
            StrikeQuality = DuckDbPersistenceHelpers.ReadNullableEnum<StrikeQuality>(reader, "StrikeQuality"),
            ShotShape = DuckDbPersistenceHelpers.ReadNullableEnum<ShotShape>(reader, "ShotShape"),
            LieType = DuckDbPersistenceHelpers.ReadNullableString(reader, "LieType"),
            SwingType = DuckDbPersistenceHelpers.ReadNullableEnum<SwingType>(reader, "SwingType"),
            TargetDistance = DuckDbPersistenceHelpers.ReadNullableDistance(reader, "TargetDistanceYards"),
            IsIncluded = DuckDbPersistenceHelpers.ReadBoolean(reader, "IsIncluded"),
            ExclusionReason = DuckDbPersistenceHelpers.ReadNullableString(reader, "ExclusionReason"),
            IsEstimated = DuckDbPersistenceHelpers.ReadBoolean(reader, "IsEstimated"),
            Notes = DuckDbPersistenceHelpers.ReadNullableString(reader, "Notes"),
            RawImportData = DuckDbPersistenceHelpers.ReadNullableString(reader, "RawImportData"),
            CreatedAt = DuckDbPersistenceHelpers.ReadDateTimeOffset(reader, "CreatedAt"),
            UpdatedAt = DuckDbPersistenceHelpers.ReadDateTimeOffset(reader, "UpdatedAt"),
        };

    private static string? BuildSearchPattern(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return null;
        }

        return $"%{searchText.Trim().ToLower(CultureInfo.InvariantCulture)}%";
    }
}
