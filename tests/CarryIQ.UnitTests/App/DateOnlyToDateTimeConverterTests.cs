using System.Globalization;
using CarryIQ.App;

namespace CarryIQ.UnitTests.App;

public class DateOnlyToDateTimeConverterTests
{
    [Fact]
    public void ConvertAndConvertBackRoundTripADateOnly()
    {
        var converter = new DateOnlyToDateTimeConverter();
        var date = new DateOnly(2026, 7, 19);

        var converted = converter.Convert(date, typeof(DateTime), null, CultureInfo.InvariantCulture);

        Assert.IsType<DateTime>(converted);
        Assert.Equal(new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Unspecified), converted);
        Assert.Equal(date, converter.ConvertBack(converted, typeof(DateOnly), null, CultureInfo.InvariantCulture));
    }
}
