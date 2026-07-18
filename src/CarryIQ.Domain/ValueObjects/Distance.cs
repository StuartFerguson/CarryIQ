namespace CarryIQ.Domain;

public readonly record struct Distance(decimal Yards)
{
    private const decimal MetresPerYard = 0.9144m;

    public static Distance Zero => new(0m);

    public static Distance FromYards(decimal yards) => new(yards);

    public static Distance FromMetres(decimal metres) => new(metres / MetresPerYard);

    public static Distance From(decimal value, DistanceUnit unit) => unit switch
    {
        DistanceUnit.Yards => FromYards(value),
        DistanceUnit.Metres => FromMetres(value),
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
    };

    public decimal ToUnit(DistanceUnit unit) => unit switch
    {
        DistanceUnit.Yards => Yards,
        DistanceUnit.Metres => Yards * MetresPerYard,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null),
    };
}
