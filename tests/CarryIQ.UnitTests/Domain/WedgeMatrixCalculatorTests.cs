using System.Globalization;

namespace CarryIQ.UnitTests.Domain;

public class WedgeMatrixCalculatorTests
{
    [Fact]
    public void CalculateBuildsRowsForActiveWedgesAndMapsSetupCells()
    {
        var golferProfileId = Guid.NewGuid();
        var clubs = new[]
        {
            CreateClub(golferProfileId, "Gap Wedge", ClubType.GapWedge, sortOrder: 1, isActive: true),
            CreateClub(golferProfileId, "Sand Wedge", ClubType.SandWedge, sortOrder: 2, isActive: false),
            CreateClub(golferProfileId, "9 Iron", ClubType.Iron, sortOrder: 3, isActive: true),
        };
        var references = new[]
        {
            CreateReference(golferProfileId, clubs[0].Id, "A1", 58m, 55m, 2.2m, 7),
            CreateReference(golferProfileId, clubs[0].Id, "A2", 62m, 59m, 2.4m, 6),
            CreateReference(golferProfileId, clubs[0].Id, "A3", 66m, 63m, 2.8m, 5),
            CreateReference(golferProfileId, clubs[1].Id, "A1", 78m, 74m, 3.4m, 5),
        };

        var result = WedgeMatrixCalculator.Calculate(clubs, references, includeInactive: false);

        Assert.Single(result.Rows);
        Assert.Equal("Gap Wedge", result.Rows[0].ClubName);
        Assert.NotNull(result.Rows[0].A1);
        Assert.Equal(58m, result.Rows[0].A1!.TargetDistance!.Value.Yards);
        Assert.Equal(55m, result.Rows[0].A1!.AverageCarry!.Value.Yards);
        Assert.Equal(7, result.Rows[0].A1!.SampleSize);
        Assert.NotNull(result.Rows[0].A2);
        Assert.NotNull(result.Rows[0].A3);
    }

    [Fact]
    public void CalculateIncludesInactiveWedgesWhenRequested()
    {
        var golferProfileId = Guid.NewGuid();
        var activeClub = CreateClub(golferProfileId, "Gap Wedge", ClubType.GapWedge, sortOrder: 1, isActive: true);
        var inactiveClub = CreateClub(golferProfileId, "Lob Wedge", ClubType.LobWedge, sortOrder: 2, isActive: false);
        var references = new[]
        {
            CreateReference(golferProfileId, activeClub.Id, "A1", 58m, 55m, 2.2m, 7),
            CreateReference(golferProfileId, inactiveClub.Id, "A1", 82m, 79m, 4.1m, 4),
        };

        var result = WedgeMatrixCalculator.Calculate(clubs: [activeClub, inactiveClub], references, includeInactive: true);

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal("Gap Wedge", result.Rows[0].ClubName);
        Assert.Equal("Lob Wedge", result.Rows[1].ClubName);
        Assert.False(result.Rows[1].IsActive);
    }

    [Fact]
    public void CalculateLeavesMissingSetupCellsNull()
    {
        var golferProfileId = Guid.NewGuid();
        var club = CreateClub(golferProfileId, "Sand Wedge", ClubType.SandWedge, sortOrder: 1, isActive: true);
        var references = new[]
        {
            CreateReference(golferProfileId, club.Id, "A1", 75m, 72m, 3.3m, 5),
        };

        var result = WedgeMatrixCalculator.Calculate([club], references, includeInactive: false);

        Assert.Single(result.Rows);
        Assert.NotNull(result.Rows[0].A1);
        Assert.Null(result.Rows[0].A2);
        Assert.Null(result.Rows[0].A3);
    }

    private static WedgeMatrixClub CreateClub(Guid golferProfileId, string name, ClubType clubType, int sortOrder, bool isActive) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            ClubType = clubType,
            SortOrder = sortOrder,
            IsActive = isActive,
        };

    private static WedgeSwingReference CreateReference(
        Guid golferProfileId,
        Guid clubId,
        string setupLabel,
        decimal targetDistanceYards,
        decimal averageCarryYards,
        decimal standardDeviationYards,
        int sampleSize) =>
        new()
        {
            Id = Guid.NewGuid(),
            GolferProfileId = golferProfileId,
            ClubId = clubId,
            SwingLabel = setupLabel,
            SwingType = SwingType.Full,
            ClockPosition = null,
            TargetDistance = Distance.FromYards(targetDistanceYards),
            AverageCarry = Distance.FromYards(averageCarryYards),
            CarryStandardDeviation = Distance.FromYards(standardDeviationYards),
            SampleSize = sampleSize,
            IsManualOverride = false,
            UpdatedAt = DateTimeOffset.Parse("2026-07-20T10:00:00Z", CultureInfo.InvariantCulture),
        };
}
