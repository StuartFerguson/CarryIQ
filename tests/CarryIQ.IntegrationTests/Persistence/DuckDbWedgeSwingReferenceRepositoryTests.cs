using System.Globalization;

namespace CarryIQ.IntegrationTests.Persistence;

public class DuckDbWedgeSwingReferenceRepositoryTests
{
    [Fact]
    public async Task SearchReturnsSeededWedgeReferences()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var clubId = await scope.SeedClubAsync("Gap Wedge", ClubType.GapWedge, isActive: true, sortOrder: 1);
        await scope.SeedWedgeSwingReferenceAsync(clubId, "A1", null, 58m, 55m, 2.2m, 7, false);
        await scope.SeedWedgeSwingReferenceAsync(clubId, "A2", "12:00", 62m, 59m, 2.4m, 6, true);

        var references = await scope.WedgeSwingReferences.SearchAsync(scope.DefaultGolferProfileId, CancellationToken.None);

        Assert.Equal(2, references.Count);
        Assert.Equal("A1", references[0].SwingLabel);
        Assert.Equal(58m, references[0].TargetDistance!.Value.Yards);
        Assert.Equal("A2", references[1].SwingLabel);
        Assert.Equal("12:00", references[1].ClockPosition);
        Assert.True(references[1].IsManualOverride);
    }

    [Fact]
    public async Task SaveUpdatesAnExistingWedgeReference()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var clubId = await scope.SeedClubAsync("Gap Wedge", ClubType.GapWedge, isActive: true, sortOrder: 1);
        var referenceId = await scope.SeedWedgeSwingReferenceAsync(clubId, "A1", null, 58m, 55m, 2.2m, 7, false);

        var reference = (await scope.WedgeSwingReferences.SearchAsync(scope.DefaultGolferProfileId, CancellationToken.None)).Single();
        await scope.WedgeSwingReferences.SaveAsync(reference with
        {
            Id = referenceId,
            AverageCarry = Distance.FromYards(57m),
            CarryStandardDeviation = Distance.FromYards(1.9m),
            SampleSize = 8,
            IsManualOverride = true,
            UpdatedAt = DateTimeOffset.Parse("2026-07-20T10:30:00Z", CultureInfo.InvariantCulture),
        }, CancellationToken.None);

        var results = await scope.WedgeSwingReferences.SearchAsync(scope.DefaultGolferProfileId, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(57m, results[0].AverageCarry!.Value.Yards);
        Assert.Equal(1.9m, results[0].CarryStandardDeviation!.Value.Yards);
        Assert.Equal(8, results[0].SampleSize);
        Assert.True(results[0].IsManualOverride);
    }
}
