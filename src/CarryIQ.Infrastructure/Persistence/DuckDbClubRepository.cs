using System.Data.Common;
using System.Globalization;
using System.Text;

namespace CarryIQ.Infrastructure;

public sealed class DuckDbClubRepository : IClubRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public DuckDbClubRepository(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Club?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, GolferProfileId, Name, ClubType, Manufacturer, Model, Loft, Shaft, ShaftFlex,
                   LengthYards, IsActive, SortOrder, Notes, CreatedAt, UpdatedAt
            FROM Clubs
            WHERE Id = $id;
            """;
        DuckDbPersistenceHelpers.AddParameter(command, "$id", id);

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
        var sql = new StringBuilder("""
            SELECT Id, Name, ClubType, SortOrder, IsActive
            FROM Clubs
            WHERE 1 = 1
            """);

        var searchPattern = BuildSearchPattern(criteria.SearchText);
        if (criteria.GolferProfileId is Guid golferProfileId)
        {
            sql.AppendLine("AND GolferProfileId = $golferProfileId");
        }

        if (criteria.ActiveOnly == true)
        {
            sql.AppendLine("AND IsActive = TRUE");
        }

        if (searchPattern is not null)
        {
            sql.AppendLine("""
                AND (
                    LOWER(Name) LIKE $searchPattern
                    OR LOWER(COALESCE(Manufacturer, '')) LIKE $searchPattern
                    OR LOWER(COALESCE(Model, '')) LIKE $searchPattern
                    OR LOWER(COALESCE(Notes, '')) LIKE $searchPattern
                )
                """);
        }

        sql.AppendLine("ORDER BY SortOrder, Name;");

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        if (criteria.GolferProfileId is Guid profileId)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$golferProfileId", profileId);
        }

        if (searchPattern is not null)
        {
            DuckDbPersistenceHelpers.AddParameter(command, "$searchPattern", searchPattern);
        }

        var results = new List<ClubSummary>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ClubSummary(
                DuckDbPersistenceHelpers.ReadGuid(reader, "Id"),
                DuckDbPersistenceHelpers.ReadString(reader, "Name"),
                DuckDbPersistenceHelpers.ReadEnum<ClubType>(reader, "ClubType"),
                DuckDbPersistenceHelpers.ReadInt32(reader, "SortOrder"),
                DuckDbPersistenceHelpers.ReadBoolean(reader, "IsActive")));
        }

        return results;
    }

    public async Task SaveAsync(Club club, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await UpsertAsync(connection, transaction, club, cancellationToken);
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
                DELETE FROM WedgeSwingReferences WHERE ClubId = $id;
                DELETE FROM Shots WHERE ClubId = $id;
                DELETE FROM Clubs WHERE Id = $id;
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
        Club club,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
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

        DuckDbPersistenceHelpers.AddParameter(command, "$id", club.Id);
        DuckDbPersistenceHelpers.AddParameter(command, "$golferProfileId", club.GolferProfileId);
        DuckDbPersistenceHelpers.AddParameter(command, "$name", club.Name);
        DuckDbPersistenceHelpers.AddParameter(command, "$clubType", (int)club.ClubType);
        DuckDbPersistenceHelpers.AddParameter(command, "$manufacturer", club.Manufacturer);
        DuckDbPersistenceHelpers.AddParameter(command, "$model", club.Model);
        DuckDbPersistenceHelpers.AddParameter(command, "$loft", club.Loft);
        DuckDbPersistenceHelpers.AddParameter(command, "$shaft", club.Shaft);
        DuckDbPersistenceHelpers.AddParameter(command, "$shaftFlex", club.ShaftFlex);
        DuckDbPersistenceHelpers.AddParameter(command, "$lengthYards", DuckDbPersistenceHelpers.ToDbValue(club.Length));
        DuckDbPersistenceHelpers.AddParameter(command, "$isActive", club.IsActive);
        DuckDbPersistenceHelpers.AddParameter(command, "$sortOrder", club.SortOrder);
        DuckDbPersistenceHelpers.AddParameter(command, "$notes", club.Notes);
        DuckDbPersistenceHelpers.AddParameter(command, "$createdAt", DuckDbPersistenceHelpers.ToDbValue(club.CreatedAt));
        DuckDbPersistenceHelpers.AddParameter(command, "$updatedAt", DuckDbPersistenceHelpers.ToDbValue(club.UpdatedAt));

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

    private static Club MapClub(DbDataReader reader) =>
        new()
        {
            Id = DuckDbPersistenceHelpers.ReadGuid(reader, "Id"),
            GolferProfileId = DuckDbPersistenceHelpers.ReadGuid(reader, "GolferProfileId"),
            Name = DuckDbPersistenceHelpers.ReadString(reader, "Name"),
            ClubType = DuckDbPersistenceHelpers.ReadEnum<ClubType>(reader, "ClubType"),
            Manufacturer = DuckDbPersistenceHelpers.ReadNullableString(reader, "Manufacturer"),
            Model = DuckDbPersistenceHelpers.ReadNullableString(reader, "Model"),
            Loft = DuckDbPersistenceHelpers.ReadNullableDecimal(reader, "Loft"),
            Shaft = DuckDbPersistenceHelpers.ReadNullableString(reader, "Shaft"),
            ShaftFlex = DuckDbPersistenceHelpers.ReadNullableString(reader, "ShaftFlex"),
            Length = DuckDbPersistenceHelpers.ReadNullableDistance(reader, "LengthYards"),
            IsActive = DuckDbPersistenceHelpers.ReadBoolean(reader, "IsActive"),
            SortOrder = DuckDbPersistenceHelpers.ReadInt32(reader, "SortOrder"),
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
