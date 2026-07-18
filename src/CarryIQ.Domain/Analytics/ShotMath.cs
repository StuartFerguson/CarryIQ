namespace CarryIQ.Domain;

public static class ShotMath
{
    public static decimal? CalculateSmashFactor(Speed? ballSpeed, Speed? clubSpeed)
    {
        if (ballSpeed is null || clubSpeed is null || clubSpeed.Value.MilesPerHour <= 0m)
        {
            return null;
        }

        return ballSpeed.Value.MilesPerHour / clubSpeed.Value.MilesPerHour;
    }
}
