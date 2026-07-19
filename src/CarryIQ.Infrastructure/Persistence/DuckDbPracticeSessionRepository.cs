using System.Data.Common;
using System.Globalization;
using System.Text;

namespace CarryIQ.Infrastructure;

public sealed class DuckDbPracticeSessionRepository : IPracticeSessionRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public DuckDbPracticeSessionRepository(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PracticeSession?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, GolferProfileId, Name, SessionDate, StartTime, EndTime, LocationName,
                   SessionType, SurfaceType, BallType, LaunchMonitorSource, WeatherDescription,
                   TemperatureCelsius, WindSpeedMilesPerHour, WindDirection, ElevationMetres, Notes,
                   CreatedAt, UpdatedAt
            FROM PracticeSessions
            WHERE Id = $id;
            """;
        DuckDbPersistenceHelpers.AddParameter(command, "$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapPracticeSession(reader);
    }

    public async Task<IReadOnlyList<PracticeSessionSummary>> SearchAsync(
        SessionSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var sql = new StringBuilder("""
            SELECT ps.Id, ps.GolferProfileId, ps.Name, ps.SessionDate, ps.SessionType,
                   ps.LocationName, ps.LaunchMonitorSource,
                   COUNT(s.Id) AS ShotCount,
                   COALESCE(SUM(CASE WHEN s.IsIncluded THEN 1 ELSE 0 END), 0) AS ValidShotCount
            FROM PracticeSessions ps
            LEFT JOIN Shots s ON s.PracticeSessionId = ps.Id
            WHERE 1 = 1
            """);

        var searchPattern = BuildSearchPattern(criteria.SearchText);
        if (criteria.GolferProfileId is Guid golferProfileId)
        {
            sql.AppendLine("AND ps.GolferProfileId = $golferProfileId");
        }

        if (criteria.StartDate is DateOnly startDate)
        {
            sql.AppendLine("AND CAST(ps.SessionDate AS TIMESTAMP) >= $startDate");
        }

        if (criteria.EndDate is DateOnly endDate)
        {
            sql.AppendLine("AND CAST(ps.SessionDate AS TIMESTAMP) < $endDateExclusive");
        }

        if (searchPattern is not null)
        {
            sql.AppendLine("""
                AND (
                    LOWER(ps.Name) LIKE $searchPattern
                    OR LOWER(COALESCE(ps.LocationName, '')) LIKE $searchPattern
                    OR LOWER(COALESCE(ps.LaunchMonitorSource, '')) LIKE $searchPattern
                    OR LOWER(COALESCE(ps.BallType, '')) LIKE $searchPattern
                    OR LOWER(COALESCE(ps.Notes, '')) LIKE $searchPattern
                )
                """);
        }

        sql.AppendLine("""
            GROUP BY ps.Id, ps.GolferProfileId, ps.Name, ps.SessionDate, ps.SessionType,
                     ps.LocationName, ps.LaunchMonitorSource
            ORDER BY ps.SessionDate DESC, ps.Name;
            """);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();

        if (criteria.GolferProfileId is Guid profileId)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$golferProfileId", profileId);
        }

        if (criteria.StartDate is DateOnly start)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$startDate", DateTime.SpecifyKind(start.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
        }

        if (criteria.EndDate is DateOnly end)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$endDateExclusive", DateTime.SpecifyKind(end.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc));
        }

        if (searchPattern is not null)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$searchPattern", searchPattern);
        }

        var results = new List<PracticeSessionSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new PracticeSessionSummary(
                DuckDbPersistenceHelpers.ReadGuid(reader, "Id"),
                DuckDbPersistenceHelpers.ReadGuid(reader, "GolferProfileId"),
                DuckDbPersistenceHelpers.ReadString(reader, "Name"),
                DuckDbPersistenceHelpers.ReadDateOnly(reader, "SessionDate"),
                DuckDbPersistenceHelpers.ReadEnum<SessionType>(reader, "SessionType"),
                DuckDbPersistenceHelpers.ReadNullableString(reader, "LocationName"),
                DuckDbPersistenceHelpers.ReadNullableString(reader, "LaunchMonitorSource"),
                (int)DuckDbPersistenceHelpers.ReadInt64(reader, "ShotCount"),
                (int)DuckDbPersistenceHelpers.ReadInt64(reader, "ValidShotCount")));
        }

        return results;
    }

    public async Task SaveAsync(PracticeSession session, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await UpsertAsync(connection, transaction, session, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteNonQueryAsync(connection, transaction, """
                DELETE FROM Shots WHERE PracticeSessionId = $id;
                DELETE FROM PracticeSessions WHERE Id = $id;
                """, id, cancellationToken);
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
        PracticeSession session,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO PracticeSessions (
                Id, GolferProfileId, Name, SessionDate, StartTime, EndTime, LocationName,
                SessionType, SurfaceType, BallType, LaunchMonitorSource, WeatherDescription,
                TemperatureCelsius, WindSpeedMilesPerHour, WindDirection, ElevationMetres, Notes,
                CreatedAt, UpdatedAt)
            VALUES (
                $id, $golferProfileId, $name, $sessionDate, $startTime, $endTime, $locationName,
                $sessionType, $surfaceType, $ballType, $launchMonitorSource, $weatherDescription,
                $temperatureCelsius, $windSpeedMilesPerHour, $windDirection, $elevationMetres, $notes,
                $createdAt, $updatedAt)
            ON CONFLICT (Id) DO UPDATE SET
                GolferProfileId = excluded.GolferProfileId,
                Name = excluded.Name,
                SessionDate = excluded.SessionDate,
                StartTime = excluded.StartTime,
                EndTime = excluded.EndTime,
                LocationName = excluded.LocationName,
                SessionType = excluded.SessionType,
                SurfaceType = excluded.SurfaceType,
                BallType = excluded.BallType,
                LaunchMonitorSource = excluded.LaunchMonitorSource,
                WeatherDescription = excluded.WeatherDescription,
                TemperatureCelsius = excluded.TemperatureCelsius,
                WindSpeedMilesPerHour = excluded.WindSpeedMilesPerHour,
                WindDirection = excluded.WindDirection,
                ElevationMetres = excluded.ElevationMetres,
                Notes = excluded.Notes,
                CreatedAt = excluded.CreatedAt,
                UpdatedAt = excluded.UpdatedAt;
            """;

        DuckDbPersistenceHelpers.AddParameter(command, "$id", session.Id);
        DuckDbPersistenceHelpers.AddParameter(command, "$golferProfileId", session.GolferProfileId);
        DuckDbPersistenceHelpers.AddParameter(command, "$name", session.Name);
        DuckDbPersistenceHelpers.AddParameter(command, "$sessionDate", DuckDbPersistenceHelpers.ToDbValue(session.SessionDate));
        DuckDbPersistenceHelpers.AddParameter(command, "$startTime", DuckDbPersistenceHelpers.ToDbValue(session.StartTime));
        DuckDbPersistenceHelpers.AddParameter(command, "$endTime", DuckDbPersistenceHelpers.ToDbValue(session.EndTime));
        DuckDbPersistenceHelpers.AddParameter(command, "$locationName", session.LocationName);
        DuckDbPersistenceHelpers.AddParameter(command, "$sessionType", (int)session.SessionType);
        DuckDbPersistenceHelpers.AddParameter(command, "$surfaceType", (int)session.SurfaceType);
        DuckDbPersistenceHelpers.AddParameter(command, "$ballType", session.BallType);
        DuckDbPersistenceHelpers.AddParameter(command, "$launchMonitorSource", session.LaunchMonitorSource);
        DuckDbPersistenceHelpers.AddParameter(command, "$weatherDescription", session.WeatherDescription);
        DuckDbPersistenceHelpers.AddParameter(command, "$temperatureCelsius", session.TemperatureCelsius);
        DuckDbPersistenceHelpers.AddParameter(command, "$windSpeedMilesPerHour", DuckDbPersistenceHelpers.ToDbValue(session.WindSpeed));
        DuckDbPersistenceHelpers.AddParameter(command, "$windDirection", session.WindDirection);
        DuckDbPersistenceHelpers.AddParameter(command, "$elevationMetres", session.ElevationMetres);
        DuckDbPersistenceHelpers.AddParameter(command, "$notes", session.Notes);
        DuckDbPersistenceHelpers.AddParameter(command, "$createdAt", DuckDbPersistenceHelpers.ToDbValue(session.CreatedAt));
        DuckDbPersistenceHelpers.AddParameter(command, "$updatedAt", DuckDbPersistenceHelpers.ToDbValue(session.UpdatedAt));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(
        DbConnection connection,
        DbTransaction transaction,
        string sql,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        DuckDbPersistenceHelpers.AddParameter(command, "$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static PracticeSession MapPracticeSession(DbDataReader reader) =>
        new()
        {
            Id = DuckDbPersistenceHelpers.ReadGuid(reader, "Id"),
            GolferProfileId = DuckDbPersistenceHelpers.ReadGuid(reader, "GolferProfileId"),
            Name = DuckDbPersistenceHelpers.ReadString(reader, "Name"),
            SessionDate = DuckDbPersistenceHelpers.ReadDateOnly(reader, "SessionDate"),
            StartTime = DuckDbPersistenceHelpers.ReadNullableTimeOnly(reader, "StartTime"),
            EndTime = DuckDbPersistenceHelpers.ReadNullableTimeOnly(reader, "EndTime"),
            LocationName = DuckDbPersistenceHelpers.ReadNullableString(reader, "LocationName"),
            SessionType = DuckDbPersistenceHelpers.ReadEnum<SessionType>(reader, "SessionType"),
            SurfaceType = DuckDbPersistenceHelpers.ReadEnum<SurfaceType>(reader, "SurfaceType"),
            BallType = DuckDbPersistenceHelpers.ReadNullableString(reader, "BallType"),
            LaunchMonitorSource = DuckDbPersistenceHelpers.ReadNullableString(reader, "LaunchMonitorSource"),
            WeatherDescription = DuckDbPersistenceHelpers.ReadNullableString(reader, "WeatherDescription"),
            TemperatureCelsius = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "TemperatureCelsius"),
            WindSpeed = DuckDbPersistenceHelpers.ReadNullableSpeed(reader, "WindSpeedMilesPerHour"),
            WindDirection = DuckDbPersistenceHelpers.ReadNullableString(reader, "WindDirection"),
            ElevationMetres = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "ElevationMetres"),
            Notes = DuckDbPersistenceHelpers.ReadNullableString(reader, "Notes"),
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
