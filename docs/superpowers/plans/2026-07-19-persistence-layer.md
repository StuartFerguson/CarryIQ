# Persistence Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace bootstrap-only persistence with DuckDB-backed repositories and a versioned schema upgrade path for clubs, practice sessions, shots, and related seed data.

**Architecture:** Keep the repository layer explicit and raw-SQL. Add a migration runner that applies ordered SQL scripts from `src/CarryIQ.Infrastructure/Migrations` and records applied versions in `SchemaVersion`, then keep `DuckDbDatabaseInitializer` focused on directory creation, migration execution, and starter seeding. Build small repository classes that map rows to domain objects directly with parameterized SQL, transactions, and cancellation tokens. Do not introduce an ORM or a large generic data-access abstraction.

**Tech Stack:** .NET 10, DuckDB.NET.Data.Full, System.Data.Common, xUnit, local temporary database files, WPF app startup via Microsoft.Extensions.Hosting/DependencyInjection.

## Global Constraints

- Windows 10 or later
- .NET 10
- WPF
- x64
- Local single-user application
- Offline-first
- Local database storage
- Nullable reference types enabled
- Implicit usings enabled
- Central package management preferred
- Use System.Text.Json
- Use DuckDB through repository interfaces
- Keep importers isolated behind `IShotFileImporter`
- Integration tests must create isolated temporary databases

---

### Task 1: Add versioned migration plumbing and keep initialization idempotent

**Files:**
- Create: `src/CarryIQ.Infrastructure/Bootstrap/DuckDbMigrationRunner.cs`
- Create: `src/CarryIQ.Infrastructure/Migrations/001_initial.sql`
- Create: `src/CarryIQ.Infrastructure/Migrations/002_add_search_indexes.sql`
- Create: `tests/CarryIQ.IntegrationTests/Persistence/TestScope.cs`
- Modify: `src/CarryIQ.Infrastructure/Bootstrap/DuckDbDatabaseInitializer.cs`
- Modify: `src/CarryIQ.Infrastructure/CarryIQ.Infrastructure.csproj`
- Modify: `tests/CarryIQ.IntegrationTests/Persistence/DuckDbDatabaseInitializerTests.cs`

**Interfaces:**
- Consumes: `IApplicationPaths`, `IDatabaseConnectionFactory`
- Produces: `DuckDbMigrationRunner.ApplyPendingMigrationsAsync(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken)`

- [ ] **Step 1: Write the failing integration test for upgrade behavior**

```csharp
[Fact]
public async Task InitializeAppliesPendingMigrationsWithoutBreakingSeedData()
{
    using var scope = new TestScope();
    await scope.CreateVersion1DatabaseAsync();

    await scope.Initializer.InitializeAsync(CancellationToken.None);

    await using var connection = scope.OpenConnection();
    Assert.Equal(2L, await scope.ScalarAsync<long>(connection, "SELECT MAX(Version) FROM SchemaVersion;"));
    Assert.Equal(1L, await scope.ScalarAsync<long>(connection, "SELECT COUNT(*) FROM GolferProfiles;"));
    Assert.Equal(15L, await scope.ScalarAsync<long>(connection, "SELECT COUNT(*) FROM Clubs;"));
}
```

`TestScope.cs` should expose the shared integration-test harness used by all three repository test files:

```csharp
public sealed class TestScope : IDisposable
{
    public DuckDbDatabaseInitializer Initializer { get; }
    public IClubRepository Clubs { get; }
    public IPracticeSessionRepository Sessions { get; }
    public IShotRepository Shots { get; }
    public Guid DefaultGolferProfileId { get; }

    public DbConnection OpenConnection();
    public Task<T> ScalarAsync<T>(DbConnection connection, string sql);
    public Task CreateVersion1DatabaseAsync();
    public Task<Guid> SeedClubAsync(string name, ClubType clubType, bool isActive, int sortOrder);
    public Task<Guid> SeedPracticeSessionAsync();
    public Task<Shot> SeedShotAsync(Guid practiceSessionId, Guid clubId);
}
```

`CreateVersion1DatabaseAsync()` should execute `001_initial.sql` directly against the temp database and rely on that script to create the version-1 row before the initializer runs.

- [ ] **Step 2: Run the test to confirm the current bootstrap cannot upgrade**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter InitializeAppliesPendingMigrationsWithoutBreakingSeedData -v minimal`
Expected: FAIL because there is no migration runner yet.

- [ ] **Step 3: Implement the migration runner and move schema creation into SQL files**

```csharp
internal sealed class DuckDbMigrationRunner
{
    public DuckDbMigrationRunner();

    public Task ApplyPendingMigrationsAsync(
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken);
}
```

`001_initial.sql` should create the full base schema and insert version `1` only when the version row is missing:

```sql
CREATE TABLE IF NOT EXISTS SchemaVersion (
    Version INTEGER NOT NULL,
    AppliedAtUtc TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS GolferProfiles (
    Id UUID PRIMARY KEY,
    DisplayName TEXT NOT NULL,
    HandicapIndex DECIMAL(6, 2) NULL,
    DominantHand INTEGER NOT NULL,
    DefaultDistanceUnit INTEGER NOT NULL,
    DefaultSpeedUnit INTEGER NOT NULL,
    DefaultTemperatureUnit INTEGER NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL
);

INSERT INTO SchemaVersion (Version, AppliedAtUtc)
SELECT 1, CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM SchemaVersion);
```

`002_add_search_indexes.sql` should add only non-destructive performance indexes:

```sql
CREATE INDEX IF NOT EXISTS IX_Clubs_GolferProfileId_IsActive_SortOrder
    ON Clubs (GolferProfileId, IsActive, SortOrder);

CREATE INDEX IF NOT EXISTS IX_PracticeSessions_GolferProfileId_SessionDate
    ON PracticeSessions (GolferProfileId, SessionDate);

CREATE INDEX IF NOT EXISTS IX_Shots_PracticeSessionId_ShotSequence
    ON Shots (PracticeSessionId, ShotSequence);
```

Update `DuckDbDatabaseInitializer` so it creates directories, opens a transaction, calls the migration runner, seeds the default golfer, seeds the starter bag, and commits or rolls back the whole unit of work.

Update `CarryIQ.Infrastructure.csproj` so the migration SQL files are copied to the output directory:

```xml
<ItemGroup>
  <Content Include="Migrations\*.sql">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

- [ ] **Step 4: Run the upgrade test until it passes**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter InitializeAppliesPendingMigrationsWithoutBreakingSeedData -v minimal`
Expected: PASS with `SchemaVersion` advanced to `2` and seed data preserved.

- [ ] **Step 5: Commit the migration plumbing**

```bash
git add src/CarryIQ.Infrastructure/Bootstrap/DuckDbDatabaseInitializer.cs src/CarryIQ.Infrastructure/Bootstrap/DuckDbMigrationRunner.cs src/CarryIQ.Infrastructure/CarryIQ.Infrastructure.csproj src/CarryIQ.Infrastructure/Migrations/001_initial.sql src/CarryIQ.Infrastructure/Migrations/002_add_search_indexes.sql tests/CarryIQ.IntegrationTests/Persistence/TestScope.cs tests/CarryIQ.IntegrationTests/Persistence/DuckDbDatabaseInitializerTests.cs
git commit -m "feat: add versioned duckdb migrations"
```

### Task 2: Implement club and practice-session repositories

**Files:**
- Create: `src/CarryIQ.Infrastructure/Persistence/DuckDbClubRepository.cs`
- Create: `src/CarryIQ.Infrastructure/Persistence/DuckDbPracticeSessionRepository.cs`
- Create: `tests/CarryIQ.IntegrationTests/Persistence/DuckDbClubRepositoryTests.cs`
- Create: `tests/CarryIQ.IntegrationTests/Persistence/DuckDbPracticeSessionRepositoryTests.cs`
- Modify: `tests/CarryIQ.IntegrationTests/Persistence/TestScope.cs`

**Interfaces:**
- Consumes: `IClubRepository`, `IPracticeSessionRepository`, `IDatabaseConnectionFactory`
- Produces: `DuckDbClubRepository` and `DuckDbPracticeSessionRepository` with `GetAsync`, `SearchAsync`, `SaveAsync`, and `DeleteAsync`

- [ ] **Step 1: Write the club repository round-trip and search tests**

```csharp
[Fact]
public async Task SaveAndGetClubRoundTripsAllFields()
{
    using var scope = new TestScope();
    await scope.Initializer.InitializeAsync(CancellationToken.None);

    var club = new Club
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        GolferProfileId = scope.DefaultGolferProfileId,
        Name = "7 Iron",
        ClubType = ClubType.Iron,
        Manufacturer = "Mizuno",
        Model = "JPX 923",
        Loft = 32m,
        Shaft = "Dynamic Gold",
        ShaftFlex = "S300",
        Length = Distance.FromYards(37m),
        IsActive = true,
        SortOrder = 4,
        Notes = "Benchmark club",
        CreatedAt = DateTimeOffset.Parse("2026-07-19T10:00:00Z"),
        UpdatedAt = DateTimeOffset.Parse("2026-07-19T10:05:00Z"),
    };

    await scope.Clubs.SaveAsync(club, CancellationToken.None);

    var loaded = await scope.Clubs.GetAsync(club.Id, CancellationToken.None);
    Assert.NotNull(loaded);
    Assert.Equal(club, loaded);
}
```

```csharp
[Fact]
public async Task SearchClubsProjectsSummaryFields()
{
    using var scope = new TestScope();
    await scope.Initializer.InitializeAsync(CancellationToken.None);
    await scope.SeedClubAsync("Driver", ClubType.Driver, isActive: true, sortOrder: 0);
    await scope.SeedClubAsync("Old 3 Wood", ClubType.FairwayWood, isActive: false, sortOrder: 1);

    var results = await scope.Clubs.SearchAsync(
        new ClubSearchCriteria(scope.DefaultGolferProfileId, ActiveOnly: true, SearchText: "Driver"),
        CancellationToken.None);

    Assert.Single(results);
    Assert.Equal("Driver", results[0].Name);
}
```

- [ ] **Step 2: Run the club tests to confirm the repository types are missing**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter FullyQualifiedName~DuckDbClubRepositoryTests -v minimal`
Expected: FAIL because the repository implementations do not exist yet.

- [ ] **Step 3: Implement the club and session repositories with explicit SQL and row mappers**

```csharp
public sealed class DuckDbClubRepository : IClubRepository
{
    public Task<Club?> GetAsync(Guid id, CancellationToken cancellationToken);
    public Task<IReadOnlyList<ClubSummary>> SearchAsync(ClubSearchCriteria criteria, CancellationToken cancellationToken);
    public Task SaveAsync(Club club, CancellationToken cancellationToken);
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
```

```csharp
public sealed class DuckDbPracticeSessionRepository : IPracticeSessionRepository
{
    public Task<PracticeSession?> GetAsync(Guid id, CancellationToken cancellationToken);
    public Task<IReadOnlyList<PracticeSessionSummary>> SearchAsync(SessionSearchCriteria criteria, CancellationToken cancellationToken);
    public Task SaveAsync(PracticeSession session, CancellationToken cancellationToken);
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
```

Implementation details to keep in scope:
- Use `INSERT ... ON CONFLICT (Id) DO UPDATE` for saves.
- Map `Distance` and `Speed` with the existing value object factories and unit conversions.
- Use joins or grouped subqueries for `PracticeSessionSummary.ShotCount` and `ValidShotCount` so search does not load all shots into memory.
- Treat `SessionSearchCriteria.Archived` as a no-op for v0.1 because the current schema has no archive column.
- Perform dependent deletes in a transaction so `PracticeSession.DeleteAsync` removes related shots first and `Club.DeleteAsync` does not leave orphaned shot rows.

- [ ] **Step 4: Run the club and session tests until they pass**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter "FullyQualifiedName~DuckDbClubRepositoryTests|FullyQualifiedName~DuckDbPracticeSessionRepositoryTests" -v minimal`
Expected: PASS.

- [ ] **Step 5: Commit the club and session repository work**

```bash
git add src/CarryIQ.Infrastructure/Persistence/DuckDbClubRepository.cs src/CarryIQ.Infrastructure/Persistence/DuckDbPracticeSessionRepository.cs tests/CarryIQ.IntegrationTests/Persistence/DuckDbClubRepositoryTests.cs tests/CarryIQ.IntegrationTests/Persistence/DuckDbPracticeSessionRepositoryTests.cs tests/CarryIQ.IntegrationTests/Persistence/TestScope.cs
git commit -m "feat: add club and session repositories"
```

### Task 3: Implement shot persistence and register repositories in the app host

**Files:**
- Create: `src/CarryIQ.Infrastructure/Persistence/DuckDbShotRepository.cs`
- Create: `tests/CarryIQ.IntegrationTests/Persistence/DuckDbShotRepositoryTests.cs`
- Modify: `src/CarryIQ.App/AppHost.cs`
- Modify: `tests/CarryIQ.IntegrationTests/Persistence/TestScope.cs`

**Interfaces:**
- Consumes: `IShotRepository`, `IClubRepository`, `IPracticeSessionRepository`, `IDatabaseConnectionFactory`
- Produces: `DuckDbShotRepository` with `AddAsync`, `AddRangeAsync`, `UpdateAsync`, and `SearchAsync`, plus DI registrations for all persistence services

- [ ] **Step 1: Write the shot repository round-trip and update tests**

```csharp
[Fact]
public async Task AddAndSearchShotsPreservesMeasuredValues()
{
    using var scope = new TestScope();
    await scope.Initializer.InitializeAsync(CancellationToken.None);
    var sessionId = await scope.SeedPracticeSessionAsync();
    var clubId = await scope.SeedClubAsync("7 Iron", ClubType.Iron, isActive: true, sortOrder: 4);

    var shot = new Shot
    {
        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        PracticeSessionId = sessionId,
        ClubId = clubId,
        ShotSequence = 1,
        RecordedAt = DateTimeOffset.Parse("2026-07-19T10:10:00Z"),
        Source = ShotSourceKind.Manual,
        CarryDistance = Distance.FromYards(154m),
        BallSpeed = Speed.FromMilesPerHour(118m),
        IsIncluded = true,
        IsEstimated = false,
        Notes = "Pure strike",
        CreatedAt = DateTimeOffset.Parse("2026-07-19T10:10:00Z"),
        UpdatedAt = DateTimeOffset.Parse("2026-07-19T10:10:00Z"),
    };

    await scope.Shots.AddAsync(shot, CancellationToken.None);

    var results = await scope.Shots.SearchAsync(
        new ShotSearchCriteria(PracticeSessionId: sessionId, IncludedOnly: true),
        CancellationToken.None);

    Assert.Single(results);
    Assert.Equal(154m, results[0].CarryDistance!.Yards);
    Assert.Equal(118m, results[0].BallSpeed!.MilesPerHour);
}
```

```csharp
[Fact]
public async Task UpdateShotPersistsEditedFields()
{
    using var scope = new TestScope();
    await scope.Initializer.InitializeAsync(CancellationToken.None);
    var sessionId = await scope.SeedPracticeSessionAsync();
    var clubId = await scope.SeedClubAsync("7 Iron", ClubType.Iron, isActive: true, sortOrder: 4);

    var shot = await scope.SeedShotAsync(sessionId, clubId);
    shot = shot with { Notes = "Updated note", IsIncluded = false, UpdatedAt = DateTimeOffset.Parse("2026-07-19T10:15:00Z") };

    await scope.Shots.UpdateAsync(shot, CancellationToken.None);

    var loaded = await scope.Shots.SearchAsync(new ShotSearchCriteria(PracticeSessionId: sessionId), CancellationToken.None);
    Assert.Equal("Updated note", loaded[0].Notes);
    Assert.False(loaded[0].IsIncluded);
}
```

- [ ] **Step 2: Run the shot tests to confirm the repository type is missing**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter FullyQualifiedName~DuckDbShotRepositoryTests -v minimal`
Expected: FAIL because `DuckDbShotRepository` does not exist yet.

- [ ] **Step 3: Implement shot persistence with explicit SQL and ordered search**

```csharp
public sealed class DuckDbShotRepository : IShotRepository
{
    public Task AddAsync(Shot shot, CancellationToken cancellationToken);
    public Task AddRangeAsync(IReadOnlyCollection<Shot> shots, CancellationToken cancellationToken);
    public Task UpdateAsync(Shot shot, CancellationToken cancellationToken);
    public Task<IReadOnlyList<Shot>> SearchAsync(ShotSearchCriteria criteria, CancellationToken cancellationToken);
}
```

Implementation details to keep in scope:
- `AddRangeAsync` should insert in a single transaction.
- `SearchAsync` should filter by `PracticeSessionId`, `ClubId`, date range, `IncludedOnly`, and search text without materializing unrelated rows.
- Order results by `RecordedAt` and `ShotSequence`.
- Map nullable measurement columns back to `Distance?` and `Speed?`.
- Preserve `RawImportData`, `ExclusionReason`, and `IsEstimated` during updates.

- [ ] **Step 4: Register the repositories in the WPF host**

Update `AppHost.BuildHost()` to register the new services:

```csharp
services.AddSingleton<DuckDbMigrationRunner>();
services.AddSingleton<IClubRepository, DuckDbClubRepository>();
services.AddSingleton<IPracticeSessionRepository, DuckDbPracticeSessionRepository>();
services.AddSingleton<IShotRepository, DuckDbShotRepository>();
```

Keep the existing `IApplicationPaths`, `IDatabaseConnectionFactory`, and `IDatabaseInitializer` registrations in place.

- [ ] **Step 5: Run the full integration suite**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj -v minimal`
Expected: PASS.

- [ ] **Step 6: Commit the shot persistence and host wiring**

```bash
git add src/CarryIQ.Infrastructure/Persistence/DuckDbShotRepository.cs src/CarryIQ.App/AppHost.cs tests/CarryIQ.IntegrationTests/Persistence/DuckDbShotRepositoryTests.cs tests/CarryIQ.IntegrationTests/Persistence/TestScope.cs
git commit -m "feat: wire shot persistence into the app host"
```

### Task 4: Finish verification against the solution

**Files:**
- Modify: any files required by build or test failures discovered in Tasks 1-3

**Interfaces:**
- Consumes: the completed persistence layer and host registrations
- Produces: a passing restore/build/test run for the full solution

- [ ] **Step 1: Run a solution-level build**

Run: `dotnet build CarryIQ.sln -c Release`
Expected: PASS with no warnings or errors introduced by the persistence changes.

- [ ] **Step 2: Run the full solution tests**

Run: `dotnet test CarryIQ.sln -c Release`
Expected: PASS for unit and integration tests.

- [ ] **Step 3: Fix any edge cases uncovered by the full test pass**

Typical fixes in this task should stay small and local:
- Adjust SQL projections if a mapping column name is wrong.
- Tighten a transaction boundary if a repository leaves a connection open.
- Update a test helper if the seed data assumptions were off.

- [ ] **Step 4: Commit the final verification fixes**

```bash
git add -A
git commit -m "test: verify persistence layer solution build"
```
