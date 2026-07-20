# Carry Analytics and Club Gapping Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a working analytics workspace that summarizes included shots, exposes consistency and gap calculations, and presents the results in a master-detail UI.

**Architecture:** Keep statistical math in `CarryIQ.Domain`, add a small application-facing analytics service/view model layer that loads included shots and clubs from the existing repositories, and surface the results in a dedicated WPF analytics screen. The top half of the screen will be a sortable grid of club summaries; the lower half will show details and warnings for the selected club.

**Tech Stack:** C# 13, .NET 10, WPF, CommunityToolkit.Mvvm, xUnit, DuckDB.

## Global Constraints

- Analytics use included shots by default.
- Score and gap calculations are deterministic and unit-tested.
- Gaps highlight overlap and poor spacing.
- Insufficient-sample warnings are surfaced in results.

---

### Task 1: Add analytics models and calculators

**Files:**
- Create: `src/CarryIQ.Domain/Analytics/ClubGapOption.cs`
- Create: `src/CarryIQ.Domain/Analytics/ClubGapSummary.cs`
- Create: `src/CarryIQ.Domain/Analytics/ClubAnalyticsSummary.cs`
- Create: `src/CarryIQ.Domain/Analytics/ClubAnalyticsCalculator.cs`
- Modify: `src/CarryIQ.Domain/Analytics/CarryStatistics.cs`
- Modify: `src/CarryIQ.Domain/Analytics/CarryStatisticsCalculator.cs`
- Modify: `src/CarryIQ.Domain/Analytics/ConsistencyScoreCalculator.cs`
- Test: `tests/CarryIQ.UnitTests/Domain/CarryStatisticsCalculatorTests.cs`

**Interfaces:**
- Consumes: `Distance`, `CarryStatistics`, `ConsistencyScoreCalculator`
- Produces: `ClubAnalyticsCalculator.Calculate(...)`, `ClubGapSummary`, `ClubAnalyticsSummary`, `ClubGapOption`

- [ ] **Step 1: Write the failing tests**

Add a new test for club analytics that verifies:

```csharp
var clubs = new[]
{
    ("7 Iron", new[] { Distance.FromYards(150m), Distance.FromYards(152m), Distance.FromYards(148m) }),
    ("8 Iron", new[] { Distance.FromYards(140m), Distance.FromYards(138m), Distance.FromYards(142m) }),
};

var result = ClubAnalyticsCalculator.Calculate(clubs, ClubGapOption.Median);

Assert.Equal(2, result.Clubs.Count);
Assert.Equal(10m, result.Gaps[0].GapYards);
Assert.True(result.Gaps[0].HasOverlap is false);
Assert.True(result.Gaps[0].HasWarning is false);
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~CarryStatisticsCalculatorTests -v minimal`

Expected: compile or test failure because the club analytics types do not exist yet.

- [ ] **Step 3: Write the minimal implementation**

Implement the new models and calculator so they:

```csharp
public enum ClubGapOption
{
    Mean,
    Median
}

public sealed record ClubGapSummary(
    string LowerClubName,
    string UpperClubName,
    decimal LowerCarryYards,
    decimal UpperCarryYards,
    decimal GapYards,
    bool HasOverlap,
    bool HasWarning);

public sealed record ClubAnalyticsSummary(
    string ClubName,
    CarryStatistics Statistics,
    decimal ConsistencyScore,
    int SampleWarningThreshold,
    bool HasInsufficientSamples);

public static class ClubAnalyticsCalculator
{
    public static ClubAnalyticsResult Calculate(
        IEnumerable<(string ClubName, IEnumerable<Distance> Carries)> clubs,
        ClubGapOption gapOption,
        int sampleWarningThreshold = 5);
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~CarryStatisticsCalculatorTests -v minimal`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CarryIQ.Domain/Analytics tests/CarryIQ.UnitTests/Domain/CarryStatisticsCalculatorTests.cs
git commit -m "feat: add club analytics calculations"
```

### Task 2: Add analytics loading and view model

**Files:**
- Create: `src/CarryIQ.App/AnalyticsRowViewModel.cs`
- Create: `src/CarryIQ.App/AnalyticsViewModel.cs`
- Modify: `src/CarryIQ.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/CarryIQ.App/AppHost.cs`
- Test: `tests/CarryIQ.UnitTests/App/AnalyticsViewModelTests.cs`

**Interfaces:**
- Consumes: `IShotRepository`, `IClubRepository`, `ClubAnalyticsCalculator`
- Produces: `AnalyticsViewModel` with `Rows`, `SelectedClub`, `Warnings`, `RefreshCommand`

- [ ] **Step 1: Write the failing tests**

Add tests that verify the view model:

```csharp
await viewModel.InitializeAsync(CancellationToken.None);

Assert.NotEmpty(viewModel.Rows);
Assert.Contains(viewModel.Rows, row => row.HasInsufficientSamples);
Assert.Contains(viewModel.Rows, row => row.GapStatusText.Contains("gap"));
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~AnalyticsViewModelTests -v minimal`

Expected: compile failure because the view model and row model do not exist yet.

- [ ] **Step 3: Write the minimal implementation**

Load all active clubs and included shots, group shots by club, calculate analytics, and expose a selected club detail model with warnings.

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~AnalyticsViewModelTests -v minimal`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CarryIQ.App tests/CarryIQ.UnitTests/App/AnalyticsViewModelTests.cs
git commit -m "feat: add analytics view model"
```

### Task 3: Build the analytics screen and navigation

**Files:**
- Create: `src/CarryIQ.App/Controls/AnalyticsView.xaml`
- Create: `src/CarryIQ.App/Controls/AnalyticsView.xaml.cs`
- Modify: `src/CarryIQ.App/MainWindow.xaml`
- Modify: `src/CarryIQ.App/ViewModels/MainWindowViewModel.cs`
- Modify: `tests/CarryIQ.UnitTests/App/MainWindowViewModelTests.cs`

**Interfaces:**
- Consumes: `AnalyticsViewModel`
- Produces: a master-detail analytics UI with a top grid and lower detail pane

- [ ] **Step 1: Write the failing UI-adjacent test**

Update shell tests to assert the navigation entry exists and that the screen title/summary describe analytics.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~MainWindowViewModelTests -v minimal`

Expected: failure until the new screen is wired in.

- [ ] **Step 3: Write the minimal implementation**

Add the analytics control to the shell, bind the top grid to the row list, and show selected-club details and warnings below it.

- [ ] **Step 4: Run the tests and build**

Run:

```bash
dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj -v minimal
dotnet build CarryIQ.sln -c Debug
```

Expected: both pass.

- [ ] **Step 5: Commit**

```bash
git add src/CarryIQ.App tests/CarryIQ.UnitTests
git commit -m "feat: add analytics workspace"
```
