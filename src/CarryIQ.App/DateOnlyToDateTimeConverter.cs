using System.Globalization;
using System.Windows.Data;

namespace CarryIQ.App;

public sealed class DateOnlyToDateTimeConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
            DateTime dateTime => dateTime,
            null => null,
            _ => throw new InvalidOperationException($"Unsupported date value type: {value.GetType().FullName}."),
        };
    }

    public object? ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            DateTimeOffset dateTimeOffset => DateOnly.FromDateTime(dateTimeOffset.DateTime),
            null => null,
            _ => throw new InvalidOperationException($"Unsupported date value type: {value.GetType().FullName}."),
        };
    }
}
