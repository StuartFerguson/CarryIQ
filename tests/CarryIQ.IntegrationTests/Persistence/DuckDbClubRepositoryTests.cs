using System.Data.Common;
using System.Globalization;
using DuckDB.NET.Data;

namespace CarryIQ.IntegrationTests.Persistence;

public class DuckDbClubRepositoryTests
{
    [Fact]
    public async Task SaveAndSearchReturnsClubsInDisplayOrder()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var golferProfileId = await scope.GetFirstGolferProfileIdAsync();
        var repository = scope.CreateRepository();

        var wedge = CreateClub(golferProfileId, "Hybrid 1", ClubType.Other, sortOrder: 100);
        var hybrid = CreateClub(golferProfileId, "Hybrid", ClubType.Other, sortOrder: 101);

        await repository.SaveAsync(wedge, CancellationToken.None);
        await repository.SaveAsync(hybrid, CancellationToken.None);

        var clubs = await repository.SearchAsync(
            new ClubSearchCriteria(golferProfileId, ActiveOnly: true),
            CancellationToken.None);

        var managedClubs = clubs.Where(club => club.Name is "Hybrid 1" or "Hybrid").ToArray();

        Assert.Equal(["Hybrid 1", "Hybrid"], managedClubs.Select(club => club.Name));
        Assert.Equal([100, 101], managedClubs.Select(club => club.SortOrder));
    }

    [Fact]
    public async Task DeleteAsyncMarksClubInactiveAndKeepsHistoricReference()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var golferProfileId = await scope.GetFirstGolferProfileIdAsync();
        var repository = scope.CreateRepository();

        var club = CreateClub(golferProfileId, "Utility Iron", ClubType.Other, sortOrder: 99);
        await repository.SaveAsync(club, CancellationToken.None);

        await repository.DeleteAsync(club.Id, CancellationToken.None);

        var activeClubs = await repository.SearchAsync(
            new ClubSearchCriteria(golferProfileId, ActiveOnly: true),
            CancellationToken.None);
        var allClubs = await repository.SearchAsync(
            new ClubSearchCriteria(golferProfileId),
            CancellationToken.None);

        Assert.DoesNotContain(activeClubs, item => item.Id == club.Id);
        Assert.Contains(allClubs, item => item.Id == club.Id && item.IsActive == false);
        Assert.Equal(club.Name, (await repository.GetAsync(club.Id, CancellationToken.None))!.Name);
    }

    [Fact]
    public async Task SaveAsyncRejectsDuplicateActiveClubNamesWithinTheSameBag()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var golferProfileId = await scope.GetFirstGolferProfileIdAsync();
        var repository = scope.CreateRepository();

        var first = CreateClub(golferProfileId, "Practice Iron", ClubType.Other, sortOrder: 42);
        var duplicate = CreateClub(golferProfileId, "Practice Iron", ClubType.Other, sortOrder: 43);

        await repository.SaveAsync(first, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SaveAsync(duplicate, CancellationToken.None));

        Assert.Contains("already exists", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsyncAllowsReusingANameAfterTheOriginalClubIsInactivated()
    {
        using var scope = new TestScope();
        await scope.Initializer.InitializeAsync(CancellationToken.None);

        var golferProfileId = await scope.GetFirstGolferProfileIdAsync();
        var repository = scope.CreateRepository();

        var original = CreateClub(golferProfileId, "Driving Wood", ClubType.FairwayWood, sortOrder: 20);
        var replacement = CreateClub(golferProfileId, "Driving Wood", ClubType.FairwayWood, sortOrder: 21);

        await repository.SaveAsync(original, CancellationToken.None);
        await repository.DeleteAsync(original.Id, CancellationToken.None);
        await repository.SaveAsync(replacement, CancellationToken.None);

        var activeClubs = await repository.SearchAsync(
            new ClubSearchCriteria(golferProfileId, ActiveOnly: true),
            CancellationToken.None);

        Assert.Contains(activeClubs, item => item.Id == replacement.Id && item.Name == "Driving Wood");
        Assert.DoesNotContain(activeClubs, item => item.Id == original.Id);
    }

    private static Club CreateClub(Guid golferProfileId, string name, ClubType clubType, int sortOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            GolferProfileId = golferProfileId,
            Name = name,
            ClubType = clubType,
            Loft = null,
            Manufacturer = null,
            Model = null,
            Shaft = null,
            ShaftFlex = null,
            Length = null,
            IsActive = true,
            SortOrder = sortOrder,
            Notes = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    private sealed class TestScope : IDisposable
    {
        public TestScope()
        {
            RootDirectory = Path.Combine(Path.GetTempPath(), "CarryIQ", Guid.NewGuid().ToString("N"));
            Paths = new TestApplicationPaths(RootDirectory);
            Initializer = new DuckDbDatabaseInitializer(Paths, new DuckDbConnectionFactory(Paths));
        }

        public string RootDirectory { get; }

        public TestApplicationPaths Paths { get; }

        public DuckDbDatabaseInitializer Initializer { get; }

        public DuckDbClubRepository CreateRepository() =>
            new(new DuckDbConnectionFactory(Paths));

        public async Task<Guid> GetFirstGolferProfileIdAsync()
        {
            await using var connection = new DuckDBConnection($"Data Source={Paths.DatabasePath}");
            await connection.OpenAsync(CancellationToken.None);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id FROM GolferProfiles ORDER BY CreatedAt LIMIT 1;";
            var value = await command.ExecuteScalarAsync(CancellationToken.None);
            return value is Guid guid
                ? guid
                : Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }

    private sealed class TestApplicationPaths(string rootDirectory) : IApplicationPaths
    {
        public string DataDirectory => rootDirectory;

        public string DatabasePath => Path.Combine(rootDirectory, "carryiq.duckdb");

        public string SettingsPath => Path.Combine(rootDirectory, "user-settings.json");

        public string LogsDirectory => Path.Combine(rootDirectory, "logs");

        public string BackupsDirectory => Path.Combine(rootDirectory, "backups");
    }
}
