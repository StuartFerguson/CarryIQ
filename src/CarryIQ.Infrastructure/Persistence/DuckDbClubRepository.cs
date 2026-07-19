using System.Data.Common;
using System.Globalization;

namespace CarryIQ.Infrastructure;

public sealed class DuckDbClubRepository : IClubRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public DuckDbClubRepository(IDatabaseConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<Club?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                Id, GolferProfileId, Name, ClubType, Manufacturer, Model, Loft,
                Shaft, ShaftFlex, LengthYards, IsActive, SortOrder, Notes, CreatedAt, UpdatedAt
            FROM Clubs
            WHERE Id = $id
            LIMIT 1;
            """;
        AddParameter(command, "$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapClub(reader);
    }

    public async Task<IReadOnlyList<ClubSummary>> SearchAsync(
        ClubSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = """
            SELECT Id, Name, ClubType, SortOrder, IsActive
            FROM Clubs
            WHERE 1 = 1
            """;

        if (criteria.GolferProfileId is not null)
        {
            sql += " AND GolferProfileId = $golferProfileId";
        }

        if (criteria.ActiveOnly is true)
        {
            sql += " AND IsActive = TRUE";
        }
        else if (criteria.ActiveOnly is false)
        {
            sql += " AND IsActive = FALSE";
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            sql += " AND LOWER(Name) LIKE LOWER($searchText)";
        }

        sql += " ORDER BY SortOrder, Name, CreatedAt;";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (criteria.GolferProfileId is not null)
        {
            AddParameter(command, "$golferProfileId", criteria.GolferProfileId);
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            AddParameter(command, "$searchText", $"%{criteria.SearchText.Trim()}%");
        }

        var results = new List<ClubSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ClubSummary(
                reader.GetGuid(0),
                reader.GetString(1),
                (ClubType)reader.GetInt32(2),
                reader.GetInt32(3),
                reader.GetBoolean(4)));
        }

        return results;
    }

    public async Task SaveAsync(Club club, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await EnsureUniqueActiveClubNameAsync(connection, club, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Clubs (
                Id, GolferProfileId, Name, ClubType, Manufacturer, Model, Loft,
                Shaft, ShaftFlex, LengthYards, IsActive, SortOrder, Notes, CreatedAt, UpdatedAt)
            VALUES (
                $id, $golferProfileId, $name, $clubType, $manufacturer, $model, $loft,
                $shaft, $shaftFlex, $lengthYards, $isActive, $sortOrder, $notes, $createdAt, $updatedAt)
            ON CONFLICT (Id) DO UPDATE SET
                GolferProfileId = excluded.GolferProfileId,
                Name = excluded.Name,
                ClubType = excluded.ClubType,
                Manufacturer = excluded.Manufacturer,
                Model = excluded.Model,
                Loft = excluded.Loft,
                Shaft = excluded.Shaft,
                ShaftFlex = excluded.ShaftFlex,
                LengthYards = excluded.LengthYards,
                IsActive = excluded.IsActive,
                SortOrder = excluded.SortOrder,
                Notes = excluded.Notes,
                CreatedAt = excluded.CreatedAt,
                UpdatedAt = excluded.UpdatedAt;
            """;
        AddParameters(command, club);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Clubs
            SET IsActive = FALSE, UpdatedAt = CURRENT_TIMESTAMP
            WHERE Id = $id;
            """;
        AddParameter(command, "$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureUniqueActiveClubNameAsync(
        DbConnection connection,
        Club club,
        CancellationToken cancellationToken)
    {
        if (!club.IsActive)
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM Clubs
            WHERE GolferProfileId = $golferProfileId
              AND Id <> $id
              AND IsActive = TRUE
              AND LOWER(TRIM(Name)) = LOWER(TRIM($name));
            """;
        AddParameter(command, "$golferProfileId", club.GolferProfileId);
        AddParameter(command, "$id", club.Id);
        AddParameter(command, "$name", club.Name);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (count > 0)
        {
            throw new InvalidOperationException($"An active club named '{club.Name}' already exists in this bag.");
        }
    }

    private static Club MapClub(DbDataReader reader) =>
        new()
        {
            Id = reader.GetGuid(0),
            GolferProfileId = reader.GetGuid(1),
            Name = reader.GetString(2),
            ClubType = (ClubType)reader.GetInt32(3),
            Manufacturer = reader.IsDBNull(4) ? null : reader.GetString(4),
            Model = reader.IsDBNull(5) ? null : reader.GetString(5),
            Loft = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
            Shaft = reader.IsDBNull(7) ? null : reader.GetString(7),
            ShaftFlex = reader.IsDBNull(8) ? null : reader.GetString(8),
            Length = reader.IsDBNull(9) ? null : Distance.FromYards(Convert.ToDecimal(reader.GetDouble(9), CultureInfo.InvariantCulture)),
            IsActive = reader.GetBoolean(10),
            SortOrder = reader.GetInt32(11),
            Notes = reader.IsDBNull(12) ? null : reader.GetString(12),
            CreatedAt = ToDateTimeOffset(reader.GetDateTime(13)),
            UpdatedAt = ToDateTimeOffset(reader.GetDateTime(14)),
        };

    private static DateTimeOffset ToDateTimeOffset(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Unspecified => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)),
            DateTimeKind.Utc => new DateTimeOffset(value, TimeSpan.Zero),
            _ => new DateTimeOffset(value.ToUniversalTime(), TimeSpan.Zero),
        };

    private static void AddParameters(DbCommand command, Club club)
    {
        AddParameter(command, "$id", club.Id);
        AddParameter(command, "$golferProfileId", club.GolferProfileId);
        AddParameter(command, "$name", club.Name);
        AddParameter(command, "$clubType", (int)club.ClubType);
        AddParameter(command, "$manufacturer", club.Manufacturer);
        AddParameter(command, "$model", club.Model);
        AddParameter(command, "$loft", club.Loft);
        AddParameter(command, "$shaft", club.Shaft);
        AddParameter(command, "$shaftFlex", club.ShaftFlex);
        AddParameter(command, "$lengthYards", club.Length?.Yards);
        AddParameter(command, "$isActive", club.IsActive);
        AddParameter(command, "$sortOrder", club.SortOrder);
        AddParameter(command, "$notes", club.Notes);
        AddParameter(command, "$createdAt", club.CreatedAt.UtcDateTime);
        AddParameter(command, "$updatedAt", club.UpdatedAt.UtcDateTime);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name.TrimStart('$', '@', ':');
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
