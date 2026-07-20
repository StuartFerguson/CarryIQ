# Issue 10 Dashboard Phase 1 Design

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a performance-first dashboard that shows aggregate shot metrics immediately and a recent-session view underneath, using projection-style queries so the screen stays fast.

**Architecture:** Keep the dashboard slice read-only and aggregation-driven. Add a small projection/query boundary in infrastructure that returns the handful of metrics the dashboard needs, then bind those summaries into a new dashboard view-model and WPF screen. The top of the dashboard should show metric cards for carry, consistency, left/right bias, offline spread, and sample size, while the lower section lists recent practice sessions and lets the user select one to inspect a compact session summary panel.

**Tech Stack:** C# 13, .NET 10, WPF, CommunityToolkit.Mvvm, DuckDB, xUnit.

## Global Constraints

- Dashboard data must come from aggregated/projection queries rather than loading all shots into memory.
- Left-handed and right-handed golfers must be handled correctly in bias metrics using `DominantHand`.
- Trend lines and other time-series work are out of scope for this slice.
- The dashboard must stay responsive with the existing local DuckDB database.
- Session detail on this slice is summary-only, not raw shot editing.

---

### Slice 1: Add dashboard projection models and queries

**Files:**
- Create: `src/CarryIQ.Domain/Analytics/DashboardMetrics.cs`
- Create: `src/CarryIQ.Domain/Analytics/RecentSessionSummary.cs`
- Create: `src/CarryIQ.Domain/Analytics/DashboardProjection.cs`
- Create: `src/CarryIQ.Domain/Analytics/DashboardProjectionCalculator.cs`
- Modify: `src/CarryIQ.Infrastructure/Persistence/DuckDbShotRepository.cs`
- Modify: `src/CarryIQ.Infrastructure/Persistence/DuckDbPracticeSessionRepository.cs`
- Test: `tests/CarryIQ.UnitTests/Domain/DashboardProjectionCalculatorTests.cs`
- Test: `tests/CarryIQ.IntegrationTests/Persistence/DuckDbDashboardProjectionTests.cs`

**Interfaces:**
- Consumes: `IShotRepository`, `IPracticeSessionRepository`, `Distance`, `SwingType`, `ShotShape`, `DominantHand`
- Produces: `DashboardProjection`, `DashboardMetrics`, `RecentSessionSummary`, and projection query methods that return aggregated dashboard data

- [ ] **Step 1: Write the failing projection tests**

Add a unit test that verifies the dashboard calculator returns aggregate metrics without needing raw shot rows:

```csharp
var result = DashboardProjectionCalculator.Calculate(
    shots,
    sessions,
    dominantHand: DominantHand.Right,
    recentSessionCount: 5);

Assert.Equal(5, result.RecentSessions.Count);
Assert.True(result.Metrics.AverageCarryYards > 0m);
Assert.True(result.Metrics.SampleSize > 0);
Assert.NotNull(result.RecentSessions[0].SessionDate);
```

Add an integration test that seeds a few sessions and shots, then asserts the repository-backed dashboard query returns:

```csharp
Assert.NotEmpty(result.RecentSessions);
Assert.Contains(result.RecentSessions, session => session.TotalShots > 0);
Assert.True(result.Metrics.SampleSize >= result.RecentSessions.Sum(session => session.IncludedShotCount));
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```bash
dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~DashboardProjectionCalculatorTests -v minimal
dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter FullyQualifiedName~DuckDbDashboardProjectionTests -v minimal
```

Expected: compile or test failures because the dashboard projection types do not exist yet.

- [ ] **Step 3: Implement the minimal projection layer**

Implement a dashboard calculator that:

```csharp
public static class DashboardProjectionCalculator
{
    public static DashboardProjection Calculate(
        IEnumerable<Shot> shots,
        IEnumerable<PracticeSessionSummary> recentSessions,
        DominantHand dominantHand,
        int recentSessionCount);
}
```

The calculator should:

```csharp
public sealed record DashboardMetrics(
    decimal AverageCarryYards,
    decimal CarryStandardDeviationYards,
    decimal OfflineSpreadYards,
    decimal LeftRightBiasYards,
    int SampleSize);

public sealed record RecentSessionSummary(
    Guid SessionId,
    DateOnly SessionDate,
    string Name,
    int TotalShots,
    int IncludedShotCount,
    decimal? AverageCarryYards);
```

The repository layer should expose a query path that returns the aggregated data needed for the dashboard without loading all raw shots into the UI layer. The dashboard view-model should resolve the golfer profile once, read its `DominantHand`, and pass that to the projection calculator.

- [ ] **Step 4: Run the tests to verify they pass**

Run:

```bash
dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~DashboardProjectionCalculatorTests -v minimal
dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj --filter FullyQualifiedName~DuckDbDashboardProjectionTests -v minimal
```

Expected: PASS.

---

### Slice 2: Add the dashboard view-model and shell wiring

**Files:**
- Create: `src/CarryIQ.App/DashboardViewModel.cs`
- Create: `src/CarryIQ.App/DashboardMetricCardViewModel.cs`
- Create: `src/CarryIQ.App/RecentSessionRowViewModel.cs`
- Modify: `src/CarryIQ.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/CarryIQ.App/AppHost.cs`
- Test: `tests/CarryIQ.UnitTests/App/DashboardViewModelTests.cs`
- Test: `tests/CarryIQ.UnitTests/App/MainWindowViewModelTests.cs`

**Interfaces:**
- Consumes: dashboard projection query service, `IPracticeSessionRepository`, `IShotRepository`
- Produces: `DashboardViewModel` with `MetricCards`, `RecentSessions`, `SelectedSession`, and `RefreshCommand`

- [ ] **Step 1: Write the failing view-model tests**

Add tests that verify the dashboard:

```csharp
await viewModel.InitializeAsync(CancellationToken.None);

Assert.Equal("Dashboard", viewModel.Title);
Assert.NotEmpty(viewModel.MetricCards);
Assert.NotEmpty(viewModel.RecentSessions);
Assert.NotNull(viewModel.SelectedSession);
Assert.Contains(viewModel.MetricCards, card => card.Title == "Average carry");
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```bash
dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~DashboardViewModelTests -v minimal
```

Expected: compile failure because the dashboard view-model types do not exist yet.

- [ ] **Step 3: Implement the view-model and shell registration**

Load the projection result, map it into metric cards, preserve the currently selected recent session where possible, and expose a summary footer that explains the dashboard is performance-first.

- [ ] **Step 4: Run the tests to verify they pass**

Run:

```bash
dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj -v minimal
```

Expected: PASS.

---

### Slice 3: Build the dashboard screen

**Files:**
- Create: `src/CarryIQ.App/Controls/DashboardView.xaml`
- Create: `src/CarryIQ.App/Controls/DashboardView.xaml.cs`
- Modify: `src/CarryIQ.App/MainWindow.xaml`
- Modify: `tests/CarryIQ.UnitTests/App/MainWindowViewModelTests.cs`

**Interfaces:**
- Consumes: `DashboardViewModel`
- Produces: a dashboard screen with top metric cards, recent-session list, and a compact selected-session detail panel

- [ ] **Step 1: Write the failing shell/navigation test**

Update shell tests to assert the dashboard screen binds to the new performance-first summary and shows the expected title/footer text.

- [ ] **Step 2: Run the test to verify it fails**

Run:

```bash
dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~MainWindowViewModelTests -v minimal
```

Expected: failure until the dashboard screen is wired in.

- [ ] **Step 3: Implement the dashboard view**

Render the cards across the top, recent sessions in a grid below, and a selected-session detail pane with summary-only metrics. Keep the screen visually aligned with the existing shell, but make the dashboard the most prominent landing page.

- [ ] **Step 4: Run the tests and build**

Run:

```bash
dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj -v minimal
dotnet test tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj -v minimal
dotnet build CarryIQ.sln -c Debug
```

Expected: all pass.

- [ ] **Step 5: Commit the dashboard slice**

```bash
git add src/CarryIQ.App src/CarryIQ.Domain src/CarryIQ.Infrastructure tests
git commit -m "feat: add dashboard metrics phase 1"
```
