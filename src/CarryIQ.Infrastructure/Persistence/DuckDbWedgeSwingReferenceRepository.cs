using System.Data.Common;
using System.Globalization;

namespace CarryIQ.Infrastructure;

public sealed class DuckDbWedgeSwingReferenceRepository : IWedgeSwingReferenceRepository
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public DuckDbWedgeSwingReferenceRepository(IDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<WedgeSwingReference>> SearchAsync(Guid golferProfileId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, GolferProfileId, ClubId, SwingLabel, SwingType, ClockPosition,
                   TargetDistanceYards, AverageCarryYards, CarryStandardDeviationYards,
                   SampleSize, IsManualOverride, UpdatedAt
            FROM WedgeSwingReferences
            WHERE GolferProfileId = $golferProfileId
            ORDER BY ClubId, SwingLabel, UpdatedAt DESC;
            """;
        DuckDbPersistenceHelpers.AddParameter(command, "$golferProfileId", golferProfileId);

        var results = new List<WedgeSwingReference>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new WedgeSwingReference
            {
                Id = DuckDbPersistenceHelpers.ReadGuid(reader, "Id"),
                GolferProfileId = DuckDbPersistenceHelpers.ReadGuid(reader, "GolferProfileId"),
                ClubId = DuckDbPersistenceHelpers.ReadGuid(reader, "ClubId"),
                SwingLabel = DuckDbPersistenceHelpers.ReadString(reader, "SwingLabel"),
                SwingType = DuckDbPersistenceHelpers.ReadEnum<SwingType>(reader, "SwingType"),
                ClockPosition = DuckDbPersistenceHelpers.ReadNullableString(reader, "ClockPosition"),
                TargetDistance = DuckDbPersistenceHelpers.ReadNullableDistance(reader, "TargetDistanceYards"),
                AverageCarry = DuckDbPersistenceHelpers.ReadNullableDistance(reader, "AverageCarryYards"),
                CarryStandardDeviation = DuckDbPersistenceHelpers.ReadNullableDistance(reader, "CarryStandardDeviationYards"),
                SampleSize = DuckDbPersistenceHelpers.ReadInt32(reader, "SampleSize"),
                IsManualOverride = DuckDbPersistenceHelpers.ReadBoolean(reader, "IsManualOverride"),
                UpdatedAt = DuckDbPersistenceHelpers.ReadDateTimeOffset(reader, "UpdatedAt"),
            });
        }

        return results;
    }

    public async Task SaveAsync(WedgeSwingReference reference, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = """
                    DELETE FROM WedgeSwingReferences
                    WHERE Id = $id;
                    """;
                DuckDbPersistenceHelpers.AddParameter(deleteCommand, "$id", reference.Id);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText = """
                    INSERT INTO WedgeSwingReferences (
                        Id, GolferProfileId, ClubId, SwingLabel, SwingType, ClockPosition,
                        TargetDistanceYards, AverageCarryYards, CarryStandardDeviationYards,
                        SampleSize, IsManualOverride, UpdatedAt)
                    VALUES (
                        $id, $golferProfileId, $clubId, $swingLabel, $swingType, $clockPosition,
                        $targetDistanceYards, $averageCarryYards, $carryStandardDeviationYards,
                        $sampleSize, $isManualOverride, $updatedAt);
                    """;
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$id", reference.Id);
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$golferProfileId", reference.GolferProfileId);
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$clubId", reference.ClubId);
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$swingLabel", reference.SwingLabel);
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$swingType", (int)reference.SwingType);
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$clockPosition", reference.ClockPosition);
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$targetDistanceYards", DuckDbPersistenceHelpers.ToDbValue(reference.TargetDistance));
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$averageCarryYards", DuckDbPersistenceHelpers.ToDbValue(reference.AverageCarry));
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$carryStandardDeviationYards", DuckDbPersistenceHelpers.ToDbValue(reference.CarryStandardDeviation));
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$sampleSize", reference.SampleSize);
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$isManualOverride", reference.IsManualOverride);
                DuckDbPersistenceHelpers.AddParameter(insertCommand, "$updatedAt", DuckDbPersistenceHelpers.ToDbValue(reference.UpdatedAt));
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
