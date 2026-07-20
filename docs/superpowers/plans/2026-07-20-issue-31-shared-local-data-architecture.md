# Shared Local Data Architecture Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Define and codify the local-first shared storage architecture for CarryIQ so the current WPF host and future MAUI and Blazor desktop hosts use the same DuckDB-backed data layout and data-access boundaries.

**Architecture:** Keep one canonical DuckDB database per user profile under the platform app-data root. Make the host responsible for choosing the app-data root, keep the storage path layout pure and testable, and keep business rules in domain/application layers so UI hosts never touch DuckDB directly. Offline sync stays out of scope for this slice; the design should leave room for it later without changing the repository contracts.

**Tech Stack:** .NET 10, C#, DuckDB.NET.Data.Full, xUnit.

## Global Constraints

- Local-first storage only; no cloud dependency by default.
- Preserve business rules in `CarryIQ.Domain` and `CarryIQ.Application`.
- Future MAUI and Blazor desktop hosts must be able to reuse the same storage layout and repository boundaries.
- The existing WPF host behavior must remain unchanged.

---

### Task 1: Document the shared-local-data strategy

**Files:**
- Create: `docs/decisions/0002-shared-local-data-architecture.md`
- Modify: `docs/architecture/README.md`
- Modify: `README.md`

**Interfaces:**
- Consumes: current `IApplicationPaths`, `IDatabaseInitializer`, repository abstractions, and the existing DuckDB initializer flow.
- Produces: a written architecture decision and repo-level documentation that states where the canonical data lives, who owns path resolution, and how future hosts plug in.

- [ ] **Step 1: Write the decision record**

Use this structure:

```markdown
# 0002 - Shared Local Data Architecture

CarryIQ stores all user data locally in a single DuckDB database per user profile. The host platform selects the app-data root, while CarryIQ owns the database layout and repository contracts.

## Decision

- DuckDB remains the canonical local store.
- The UI layer never accesses DuckDB directly.
- Application and domain layers own business rules.
- Platform hosts provide the app-data root through an application-paths abstraction.
- MAUI uses its platform app-data directory, WPF uses the local application data folder, and future desktop hosts must follow the same contract.

## Consequences

- Local-first behavior stays intact.
- Future sync work can be added behind the repository boundary.
- New hosts must implement the same path contract before they can read or write data.
```

- [ ] **Step 2: Update the architecture README**

Add a short section that says:

```markdown
## Shared Local Data

CarryIQ keeps one DuckDB database per user profile in the platform app-data root. The host chooses the root; the application layer defines the repository contracts; infrastructure owns the DuckDB implementation. This keeps the storage model consistent across WPF, MAUI, and future Blazor desktop hosts.
```

- [ ] **Step 3: Update the top-level README**

Adjust the architecture bullets so they explicitly say:

```markdown
- `CarryIQ.Application` contains use cases, repository contracts, and storage-path abstractions.
- `CarryIQ.Infrastructure` contains DuckDB persistence, path adapters, and bootstrap logic.
```

- [ ] **Step 4: Review the wording for contradictions**

Make sure the new decision record and README text both say the same thing about the canonical store, platform root ownership, and offline sync being deferred.

### Task 2: Extract a pure app-data path layout and test it

**Files:**
- Create: `src/CarryIQ.Infrastructure/Persistence/ApplicationDataPaths.cs`
- Modify: `src/CarryIQ.Infrastructure/Persistence/ApplicationPaths.cs`
- Create: `tests/CarryIQ.UnitTests/Persistence/ApplicationDataPathsTests.cs`

**Interfaces:**
- Consumes: the `IApplicationPaths` contract and the current `ApplicationPaths` host adapter.
- Produces: a pure path-layout helper that builds the data directory, database path, settings path, logs path, and backups path from any supplied root directory.

- [ ] **Step 1: Add the failing unit test**

```csharp
public sealed class ApplicationDataPathsTests
{
    [Fact]
    public void CreateBuildsTheCarryIqFolderLayoutFromAnyRoot()
    {
        var layout = ApplicationDataPaths.Create(@"C:\Users\stuar\AppData\Local");

        Assert.Equal(@"C:\Users\stuar\AppData\Local\CarryIQ", layout.DataDirectory);
        Assert.Equal(@"C:\Users\stuar\AppData\Local\CarryIQ\carryiq.duckdb", layout.DatabasePath);
        Assert.Equal(@"C:\Users\stuar\AppData\Local\CarryIQ\user-settings.json", layout.SettingsPath);
        Assert.Equal(@"C:\Users\stuar\AppData\Local\CarryIQ\logs", layout.LogsDirectory);
        Assert.Equal(@"C:\Users\stuar\AppData\Local\CarryIQ\backups", layout.BackupsDirectory);
    }
}
```

- [ ] **Step 2: Run the focused test and confirm it fails**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~ApplicationDataPathsTests`

Expected: FAIL because `ApplicationDataPaths` does not exist yet.

- [ ] **Step 3: Add the pure layout helper and wire the host adapter**

Use this implementation shape:

```csharp
namespace CarryIQ.Infrastructure;

public sealed record ApplicationDataPaths(
    string DataDirectory,
    string DatabasePath,
    string SettingsPath,
    string LogsDirectory,
    string BackupsDirectory)
{
    public static ApplicationDataPaths Create(string rootDirectory)
    {
        var dataDirectory = Path.Combine(rootDirectory, "CarryIQ");
        return new ApplicationDataPaths(
            dataDirectory,
            Path.Combine(dataDirectory, "carryiq.duckdb"),
            Path.Combine(dataDirectory, "user-settings.json"),
            Path.Combine(dataDirectory, "logs"),
            Path.Combine(dataDirectory, "backups"));
    }
}
```

Then update `ApplicationPaths` to delegate to the helper:

```csharp
namespace CarryIQ.Infrastructure;

public sealed class ApplicationPaths : IApplicationPaths
{
    private readonly ApplicationDataPaths _layout;

    public ApplicationPaths()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _layout = ApplicationDataPaths.Create(root);
    }

    public string DataDirectory => _layout.DataDirectory;
    public string DatabasePath => _layout.DatabasePath;
    public string SettingsPath => _layout.SettingsPath;
    public string LogsDirectory => _layout.LogsDirectory;
    public string BackupsDirectory => _layout.BackupsDirectory;
}
```

- [ ] **Step 4: Run the focused test again and confirm it passes**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~ApplicationDataPathsTests`

Expected: PASS.

- [ ] **Step 5: Run the repo test suite**

Run: `dotnet test CarryIQ.sln`

Expected: PASS with the existing analyzer warnings only.

### Task 3: Verify and finish the architecture slice

**Files:**
- Modify: `docs/superpowers/plans/2026-07-20-issue-31-shared-local-data-architecture.md`
- Modify: any file that needs wording cleanup after the test run

**Interfaces:**
- Consumes: the completed docs and path-layout helper.
- Produces: a finished architecture slice that can be reviewed independently before any MAUI or Blazor host work starts.

- [ ] **Step 1: Re-read the plan against the issue**

Confirm the implementation covers:

```markdown
- canonical local storage model
- offline-first / no-cloud-by-default stance
- platform-specific storage adapters
- data-access boundaries that avoid duplicating business rules
```

- [ ] **Step 2: Fix any mismatched wording**

If the code and docs use different names for the same concept, align them before considering the slice done.

- [ ] **Step 3: Commit the completed slice**

```bash
git add -A
git commit -m "docs: define shared local data architecture"
```
