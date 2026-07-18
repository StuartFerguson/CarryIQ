namespace CarryIQ.Domain;

public readonly record struct Speed(decimal MilesPerHour)
{
    private const decimal KilometresPerHourPerMph = 1.609344m;

    public static Speed Zero => new(0m);

    public static Speed FromMilesPerHour(decimal milesPerHour)
    {
        if (milesPerHour < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(milesPerHour), "Speed cannot be negative.");
        }

        return new Speed(milesPerHour);
    }

    public static Speed FromKilometresPerHour(decimal kilometresPerHour)
    {
        if (kilometresPerHour < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(kilometresPerHour), "Speed cannot be negative.");
        }

        return new Speed(kilometresPerHour / KilometresPerHourPerMph);
    }

    public static Speed From(decimal value, SpeedUnit unit) => unit switch
    {
        SpeedUnit.MilesPerHour => FromMilesPerHour(value),
        SpeedUnit.KilometresPerHour => FromKilometresPerHour(value),
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
    };

    public decimal ToUnit(SpeedUnit unit) => unit switch
    {
        SpeedUnit.MilesPerHour => MilesPerHour,
        SpeedUnit.KilometresPerHour => MilesPerHour * KilometresPerHourPerMph,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
    };
}
