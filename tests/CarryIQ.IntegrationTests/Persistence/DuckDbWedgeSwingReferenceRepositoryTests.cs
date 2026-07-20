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
}
