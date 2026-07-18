namespace CarryIQ.UnitTests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void DistanceConvertsBetweenYardsAndMetres()
    {
        var distance = Distance.FromYards(100m);

        Assert.Equal(91.44m, distance.ToUnit(DistanceUnit.Metres));
        Assert.Equal(100m, Distance.FromMetres(91.44m).Yards);
    }

    [Fact]
    public void SpeedConvertsBetweenMphAndKph()
    {
        var speed = Speed.FromMilesPerHour(100m);

        Assert.Equal(160.9344m, speed.ToUnit(SpeedUnit.KilometresPerHour));
        Assert.Equal(100m, Speed.FromKilometresPerHour(160.9344m).MilesPerHour);
    }
}
