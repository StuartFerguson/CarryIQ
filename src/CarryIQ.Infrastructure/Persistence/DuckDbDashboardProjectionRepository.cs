using System.Data.Common;
using System.Globalization;

namespace CarryIQ.Infrastructure;

public sealed class DuckDbDashboardProjectionRepository : IDashboardProjectionRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public DuckDbDashboardProjectionRepository(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DashboardProjectionSource> LoadAsync(Guid golferProfileId, int recentSessionCount, CancellationToken cancellationToken)
    {
        var recentSessions = await LoadRecentSessionsAsync(golferProfileId, recentSessionCount, cancellationToken);
        var shots = await LoadIncludedShotsAsync(golferProfileId, cancellationToken);

        return new DashboardProjectionSource(shots, recentSessions);
    }

    private async Task<IReadOnlyList<PracticeSessionSummary>> LoadRecentSessionsAsync(
        Guid golferProfileId,
        int recentSessionCount,
        CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ps.Id, ps.GolferProfileId, ps.Name, ps.SessionDate, ps.StartTime, ps.EndTime,
                   ps.SessionType, ps.LocationName, ps.LaunchMonitorSource, ps.IsArchived,
                   COUNT(s.Id) AS ShotCount,
                   COALESCE(SUM(CASE WHEN s.IsIncluded THEN 1 ELSE 0 END), 0) AS ValidShotCount
            FROM PracticeSessions ps
            LEFT JOIN Shots s ON s.PracticeSessionId = ps.Id
            WHERE ps.GolferProfileId = $golferProfileId
            GROUP BY ps.Id, ps.GolferProfileId, ps.Name, ps.SessionDate, ps.StartTime, ps.EndTime,
                     ps.SessionType, ps.LocationName, ps.LaunchMonitorSource, ps.IsArchived
            ORDER BY ps.SessionDate DESC, ps.StartTime DESC, ps.Name
            LIMIT $recentSessionCount;
            """;
        DuckDbPersistenceHelpers.AddParameter(command, "$golferProfileId", golferProfileId);
        DuckDbPersistenceHelpers.AddParameter(command, "$recentSessionCount", Math.Max(recentSessionCount, 0));

        var results = new List<PracticeSessionSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new PracticeSessionSummary(
                DuckDbPersistenceHelpers.ReadGuid(reader, "Id"),
                DuckDbPersistenceHelpers.ReadGuid(reader, "GolferProfileId"),
                DuckDbPersistenceHelpers.ReadString(reader, "Name"),
                DuckDbPersistenceHelpers.ReadDateOnly(reader, "SessionDate"),
                DuckDbPersistenceHelpers.ReadNullableTimeOnly(reader, "StartTime"),
                DuckDbPersistenceHelpers.ReadNullableTimeOnly(reader, "EndTime"),
                CalculateDuration(
                    DuckDbPersistenceHelpers.ReadNullableTimeOnly(reader, "StartTime"),
                    DuckDbPersistenceHelpers.ReadNullableTimeOnly(reader, "EndTime")),
                DuckDbPersistenceHelpers.ReadEnum<SessionType>(reader, "SessionType"),
                DuckDbPersistenceHelpers.ReadNullableString(reader, "LocationName"),
                DuckDbPersistenceHelpers.ReadNullableString(reader, "LaunchMonitorSource"),
                (int)DuckDbPersistenceHelpers.ReadInt64(reader, "ShotCount"),
                (int)DuckDbPersistenceHelpers.ReadInt64(reader, "ValidShotCount"),
                DuckDbPersistenceHelpers.ReadBoolean(reader, "IsArchived")));
        }

        return results;
    }

    private async Task<IReadOnlyList<Shot>> LoadIncludedShotsAsync(Guid golferProfileId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.Id, s.PracticeSessionId, s.ClubId, s.ShotSequence, s.RecordedAt, s.Source,
                   s.CarryDistanceYards, s.TotalDistanceYards, s.BallSpeedMilesPerHour, s.ClubSpeedMilesPerHour,
                   s.SmashFactor, s.LaunchAngle, s.LaunchDirection, s.ApexHeight, s.SpinRate, s.SpinAxis,
                   s.OfflineDistanceYards, s.RollDistanceYards, s.HangTime, s.AttackAngle, s.ClubPath, s.FaceAngle,
                   s.FaceToPath, s.DynamicLoft, s.StrikeQuality, s.ShotShape, s.LieType, s.SwingType,
                   s.TargetDistanceYards, s.IsIncluded, s.ExclusionReason, s.IsEstimated, s.Notes, s.RawImportData,
                   s.CreatedAt, s.UpdatedAt
            FROM Shots s
            INNER JOIN PracticeSessions ps ON ps.Id = s.PracticeSessionId
            WHERE ps.GolferProfileId = $golferProfileId
              AND s.IsIncluded = TRUE
            ORDER BY ps.SessionDate DESC, s.RecordedAt DESC, s.ShotSequence;
            """;
        DuckDbPersistenceHelpers.AddParameter(command, "$golferProfileId", golferProfileId);

        var results = new List<Shot>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapShot(reader));
        }

        return results;
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

    private static TimeSpan? CalculateDuration(TimeOnly? startTime, TimeOnly? endTime)
    {
        if (startTime is null || endTime is null)
        {
            return null;
        }

        return endTime.Value.ToTimeSpan() - startTime.Value.ToTimeSpan();
    }
}
