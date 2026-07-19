# Persistence Layer Implementation Design

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace bootstrap-only persistence with DuckDB-backed repositories and a versioned schema upgrade path for clubs, practice sessions, shots, and related support data.

**Architecture:** Keep the current local-first, raw-SQL approach. Move schema creation and migration execution into a dedicated migration runner that applies ordered SQL files against a `SchemaVersion` table, then keep the database initializer focused on directory creation and starter data seeding. Implement repository classes in infrastructure that translate between application/domain models and database rows, using parameterized SQL, transactions, and cancellation tokens throughout. The repositories should be small and explicit rather than abstracting away SQL behind a larger ORM-style layer.

**Tech Stack:** .NET 10, DuckDB.NET.Data.Full, System.Data.Common, xUnit, FluentAssertions, local temporary files for integration tests.

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

### Task 1: Add migration plumbing and version tracking

**Files:**
- Create: `src/CarryIQ.Infrastructure/Migrations/001_initial.sql`
- Create: `src/CarryIQ.Infrastructure/Migrations/002_add_search_indexes.sql`
- Create: `src/CarryIQ.Infrastructure/Bootstrap/DuckDbMigrationRunner.cs`
- Modify: `src/CarryIQ.Infrastructure/Bootstrap/DuckDbDatabaseInitializer.cs`

**Interfaces:**
- Consumes: `IDatabaseConnectionFactory`, `IApplicationPaths`
- Produces: `DuckDbMigrationRunner.ApplyAsync(DbConnection, DbTransaction, CancellationToken)` and initializer flow that calls migration execution before seeding

- [ ] **Step 1: Add a failing integration test for upgrade tracking**

The test should start from a database that contains only version `1`, then assert that the initializer advances `SchemaVersion` to the latest migration and leaves the starter data intact.

- [ ] **Step 2: Run the test to confirm the current bootstrap cannot upgrade**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter InitializeAppliesPendingMigrations -v minimal`
Expected: FAIL because there is no upgrade path beyond the initial schema create.

- [ ] **Step 3: Implement the migration runner and split schema files**

```csharp
internal sealed class DuckDbMigrationRunner
{
    public Task ApplyAsync(DbConnection connection, DbTransaction transaction, CancellationToken cancellationToken);
}
```

```sql
CREATE TABLE IF NOT EXISTS SchemaVersion (
    Version INTEGER NOT NULL,
    AppliedAtUtc TIMESTAMP NOT NULL
);

INSERT INTO SchemaVersion (Version, AppliedAtUtc)
SELECT 1, CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM SchemaVersion);
```

```sql
CREATE INDEX IF NOT EXISTS IX_Clubs_GolferProfileId_IsActive_SortOrder
    ON Clubs (GolferProfileId, IsActive, SortOrder);

CREATE INDEX IF NOT EXISTS IX_PracticeSessions_GolferProfileId_SessionDate
    ON PracticeSessions (GolferProfileId, SessionDate);

CREATE INDEX IF NOT EXISTS IX_Shots_PracticeSessionId_ShotSequence
    ON Shots (PracticeSessionId, ShotSequence);
```

- [ ] **Step 4: Run the upgrade test until it passes**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter InitializeAppliesPendingMigrations -v minimal`
Expected: PASS with `SchemaVersion` advanced to the latest applied migration.

- [ ] **Step 5: Commit the migration plumbing**

```bash
git add src/CarryIQ.Infrastructure/Bootstrap/DuckDbDatabaseInitializer.cs src/CarryIQ.Infrastructure/Bootstrap/DuckDbMigrationRunner.cs src/CarryIQ.Infrastructure/Migrations/001_initial.sql src/CarryIQ.Infrastructure/Migrations/002_add_repositories.sql tests/CarryIQ.IntegrationTests/Persistence/DuckDbDatabaseInitializerTests.cs
git commit -m "feat: add versioned duckdb migrations"
```

### Task 2: Implement club and session repositories

**Files:**
- Create: `src/CarryIQ.Infrastructure/Persistence/DuckDbClubRepository.cs`
- Create: `src/CarryIQ.Infrastructure/Persistence/DuckDbPracticeSessionRepository.cs`
- Modify: `src/CarryIQ.Infrastructure/CarryIQ.Infrastructure.csproj`
- Modify: `tests/CarryIQ.IntegrationTests/Persistence/DuckDbClubRepositoryTests.cs`
- Modify: `tests/CarryIQ.IntegrationTests/Persistence/DuckDbPracticeSessionRepositoryTests.cs`

**Interfaces:**
- Consumes: `IClubRepository`, `IPracticeSessionRepository`, `IApplicationPaths`, `IDatabaseConnectionFactory`
- Produces: repository implementations that can save, load, search, and delete clubs and practice sessions

- [ ] **Step 1: Add repository-focused integration tests**

The club test should persist a fully populated `Club` with `Id`, `GolferProfileId`, `Name`, `ClubType`, `Manufacturer`, `Model`, `Loft`, `Shaft`, `ShaftFlex`, `Length`, `IsActive`, `SortOrder`, `Notes`, `CreatedAt`, and `UpdatedAt`, then assert each field round-trips from `SaveAsync` and `GetAsync`.

```csharp
[Fact]
public async Task SearchSessionsReturnsProjectedSummaries()
{
    using var scope = new TestScope();
    await scope.Initializer.InitializeAsync(CancellationToken.None);

    var results = await scope.Sessions.SearchAsync(new SessionSearchCriteria(), CancellationToken.None);
    Assert.NotEmpty(results);
}
```

- [ ] **Step 2: Run the new tests to confirm repositories are missing**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter FullyQualifiedName~DuckDbClubRepositoryTests -v minimal`
Expected: FAIL because the repository types do not exist yet.

- [ ] **Step 3: Implement the repository classes with parameterized SQL**

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

- [ ] **Step 4: Run the repository tests until they pass**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter FullyQualifiedName~DuckDbClubRepositoryTests|FullyQualifiedName~DuckDbPracticeSessionRepositoryTests -v minimal`
Expected: PASS with round-trip persistence and projected search results.

- [ ] **Step 5: Commit the repository implementations**

```bash
git add src/CarryIQ.Infrastructure/Persistence/DuckDbClubRepository.cs src/CarryIQ.Infrastructure/Persistence/DuckDbPracticeSessionRepository.cs tests/CarryIQ.IntegrationTests/Persistence/DuckDbClubRepositoryTests.cs tests/CarryIQ.IntegrationTests/Persistence/DuckDbPracticeSessionRepositoryTests.cs
git commit -m "feat: add club and session repositories"
```

### Task 3: Implement shot repository and wire persistence registration

**Files:**
- Create: `src/CarryIQ.Infrastructure/Persistence/DuckDbShotRepository.cs`
- Modify: `src/CarryIQ.Infrastructure/CarryIQ.Infrastructure.csproj`
- Modify: `src/CarryIQ.App/AppHost.cs`
- Modify: `tests/CarryIQ.IntegrationTests/Persistence/DuckDbShotRepositoryTests.cs`

**Interfaces:**
- Consumes: `IShotRepository`, `IDatabaseConnectionFactory`
- Produces: add/update/search shot persistence and application DI registrations for all repositories

- [ ] **Step 1: Add a failing shot repository round-trip test**

The test should insert a shot with `PracticeSessionId`, `ClubId`, `ShotSequence`, `RecordedAt`, `Source`, `CarryDistance`, `BallSpeed`, `IsIncluded`, and `IsEstimated`, then assert those values round-trip through `AddAsync` and `SearchAsync`.

- [ ] **Step 2: Implement add, range-add, update, and search SQL**

```csharp
public sealed class DuckDbShotRepository : IShotRepository
{
    public Task AddAsync(Shot shot, CancellationToken cancellationToken);
    public Task AddRangeAsync(IReadOnlyCollection<Shot> shots, CancellationToken cancellationToken);
    public Task UpdateAsync(Shot shot, CancellationToken cancellationToken);
    public Task<IReadOnlyList<Shot>> SearchAsync(ShotSearchCriteria criteria, CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Register the repositories in application startup**

```csharp
services.AddSingleton<IClubRepository, DuckDbClubRepository>();
services.AddSingleton<IPracticeSessionRepository, DuckDbPracticeSessionRepository>();
services.AddSingleton<IShotRepository, DuckDbShotRepository>();
```

- [ ] **Step 4: Run the full integration suite**

Run: `dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj -v minimal`
Expected: PASS.

- [ ] **Step 5: Commit the shot repository and wiring**

```bash
git add src/CarryIQ.Infrastructure/Persistence/DuckDbShotRepository.cs src/CarryIQ.App/AppHost.cs tests/CarryIQ.IntegrationTests/Persistence/DuckDbShotRepositoryTests.cs
git commit -m "feat: wire shot persistence into app startup"
```

### Task 4: Polish search semantics and documentation

**Files:**
- Modify: `README.md`
- Modify: `docs/architecture/README.md`
- Modify: `docs/user-guide/README.md`
- Modify: `tests/CarryIQ.IntegrationTests/Persistence/DuckDbDatabaseInitializerTests.cs`

**Interfaces:**
- Consumes: the completed repositories and migration runner
- Produces: documented persistence behavior and regression coverage for idempotent initialization

- [ ] **Step 1: Add assertions for idempotent initialization plus upgrade behavior**
- [ ] **Step 2: Update the README persistence description**
- [ ] **Step 3: Verify solution-level tests**
- [ ] **Step 4: Commit the documentation and final test updates**
