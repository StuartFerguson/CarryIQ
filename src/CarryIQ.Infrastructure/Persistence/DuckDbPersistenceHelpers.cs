using System.Data.Common;
using System.Globalization;
using System.Numerics;

namespace CarryIQ.Infrastructure;

public static class DuckDbPersistenceHelpers
{
    public static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name.TrimStart('$', '@', ':');
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    public static Guid ReadGuid(DbDataReader reader, string column)
    {
        var value = reader[column];
        return value switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text, CultureInfo.InvariantCulture),
            _ => Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture),
        };
    }

    public static string ReadString(DbDataReader reader, string column) =>
        Convert.ToString(reader[column], CultureInfo.InvariantCulture) ?? string.Empty;

    public static string? ReadNullableString(DbDataReader reader, string column) =>
        reader[column] is DBNull ? null : Convert.ToString(reader[column], CultureInfo.InvariantCulture);

    public static int ReadInt32(DbDataReader reader, string column) =>
        Convert.ToInt32(ReadInt64(reader, column), CultureInfo.InvariantCulture);

    public static long ReadInt64(DbDataReader reader, string column)
    {
        var value = reader[column];
        return value switch
        {
            long longValue => longValue,
            int intValue => intValue,
            short shortValue => shortValue,
            byte byteValue => byteValue,
            BigInteger bigInteger => (long)bigInteger,
            _ => Convert.ToInt64(value, CultureInfo.InvariantCulture),
        };
    }

    public static bool ReadBoolean(DbDataReader reader, string column) =>
        Convert.ToBoolean(reader[column], CultureInfo.InvariantCulture);

    public static decimal ReadDecimal(DbDataReader reader, string column) =>
        Convert.ToDecimal(reader[column], CultureInfo.InvariantCulture);

    public static decimal? ReadNullableDecimal(DbDataReader reader, string column) =>
        reader[column] is DBNull ? null : Convert.ToDecimal(reader[column], CultureInfo.InvariantCulture);

    public static double? ReadNullableDouble(DbDataReader reader, string column) =>
        reader[column] is DBNull ? null : Convert.ToDouble(reader[column], CultureInfo.InvariantCulture);

    public static DateTimeOffset ReadDateTimeOffset(DbDataReader reader, string column)
    {
        var value = reader[column];
        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            string text => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
            _ => new DateTimeOffset(DateTime.SpecifyKind(Convert.ToDateTime(value, CultureInfo.InvariantCulture), DateTimeKind.Utc)),
        };
    }

    public static DateOnly ReadDateOnly(DbDataReader reader, string column)
    {
        var value = reader[column];
        return value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            string text => DateOnly.Parse(text, CultureInfo.InvariantCulture),
            _ => DateOnly.FromDateTime(Convert.ToDateTime(value, CultureInfo.InvariantCulture)),
        };
    }

    public static TimeOnly? ReadNullableTimeOnly(DbDataReader reader, string column)
    {
        var value = reader[column];
        return value switch
        {
            DBNull => null,
            TimeOnly timeOnly => timeOnly,
            TimeSpan timeSpan => TimeOnly.FromTimeSpan(timeSpan),
            DateTime dateTime => TimeOnly.FromDateTime(dateTime),
            string text => TimeOnly.Parse(text, CultureInfo.InvariantCulture),
            _ => TimeOnly.FromTimeSpan((TimeSpan)Convert.ChangeType(value, typeof(TimeSpan), CultureInfo.InvariantCulture)!),
        };
    }

    public static Distance? ReadNullableDistance(DbDataReader reader, string column)
    {
        var yards = ReadNullableDouble(reader, column);
        return yards is null ? null : Distance.FromYards(Convert.ToDecimal(yards.Value, CultureInfo.InvariantCulture));
    }

    public static Speed? ReadNullableSpeed(DbDataReader reader, string column)
    {
        var milesPerHour = ReadNullableDouble(reader, column);
        return milesPerHour is null ? null : Speed.FromMilesPerHour(Convert.ToDecimal(milesPerHour.Value, CultureInfo.InvariantCulture));
    }

    public static TEnum ReadEnum<TEnum>(DbDataReader reader, string column)
        where TEnum : struct, Enum
    {
        return (TEnum)Enum.ToObject(typeof(TEnum), ReadInt32(reader, column));
    }

    public static TEnum? ReadNullableEnum<TEnum>(DbDataReader reader, string column)
        where TEnum : struct, Enum
    {
        return reader[column] is DBNull ? null : ReadEnum<TEnum>(reader, column);
    }

    public static object ToDbValue(DateTimeOffset value) => DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Utc);

    public static object ToDbValue(DateOnly value) => DateTime.SpecifyKind(value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    public static object? ToDbValue(TimeOnly? value) => value is null ? DBNull.Value : value.Value.ToTimeSpan();

    public static object? ToDbValue(Distance? value) => value is null ? DBNull.Value : value.Value.Yards;

    public static object? ToDbValue(Speed? value) => value is null ? DBNull.Value : value.Value.MilesPerHour;

    public static object? ToDbValue(decimal? value) => value is null ? DBNull.Value : value.Value;

    public static object? ToDbValue(double? value) => value is null ? DBNull.Value : value.Value;

    public static object? ToDbValue(int? value) => value is null ? DBNull.Value : value.Value;
}
