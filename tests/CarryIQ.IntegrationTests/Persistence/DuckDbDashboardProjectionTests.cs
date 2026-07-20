using System.Globalization;

namespace CarryIQ.IntegrationTests.Persistence;

public class DuckDbDashboardProjectionTests
{
    [Fact]
    public async Task LoadReturnsDashboardSourceRowsThatCanBeProjectedIntoMetrics()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var clubId = await scope.SeedClubAsync("7 Iron", ClubType.Iron, isActive: true, sortOrder: 1);
        var olderSessionId = await scope.SeedPracticeSessionAsync();
        var newerSessionId = await scope.SeedPracticeSessionAsync();

        await scope.SeedShotAsync(olderSessionId, clubId, isIncluded: true);
        await scope.SeedShotAsync(olderSessionId, clubId, isIncluded: false);
        await scope.SeedShotAsync(newerSessionId, clubId, isIncluded: true);

        var repository = new DuckDbDashboardProjectionRepository(new DuckDbConnectionFactory(scope.Paths));

        var source = await repository.LoadAsync(scope.DefaultGolferProfileId, recentSessionCount: 2, CancellationToken.None);
        var projection = DashboardProjectionCalculator.Calculate(source.Shots, source.RecentSessions, DominantHand.Right, 2);

        Assert.NotEmpty(source.Shots);
        Assert.Equal(2, source.RecentSessions.Count);
        Assert.Contains(source.Shots, shot => shot.IsIncluded);
        Assert.True(projection.Metrics.SampleSize >= source.Shots.Count(shot => shot.IsIncluded));
        Assert.NotEqual(0m, projection.Metrics.AverageCarryYards);
    }
}
