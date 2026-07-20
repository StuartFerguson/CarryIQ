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
}
