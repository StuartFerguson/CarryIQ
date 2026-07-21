# Blazor Desktop Transition Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the WPF shell with a Blazor-based Windows desktop experience while keeping CarryIQ local-first, installable, and backed by the existing domain, application, and DuckDB persistence layers.

**Architecture:** Move `CarryIQ.App` to a .NET MAUI Blazor Hybrid host on Windows. Keep the current application services and repositories intact, extract the service registration into a shared extension so MAUI startup and existing host wiring use the same graph, and render the existing shell/view-model state through Razor components. The migration is staged so the desktop host stays buildable after each checkpoint: host bootstrap first, shell/navigation second, workflow screens third, then WPF teardown and documentation cleanup.

**Tech Stack:** .NET 10, C#, .NET MAUI Blazor Hybrid, Razor components, CommunityToolkit.Mvvm, DuckDB.NET.Data.Full, xUnit.

## Global Constraints

- Local-first storage only; the app must keep using the existing DuckDB-backed persistence and app-data path strategy.
- The desktop app must remain installable and runnable on Windows.
- Domain and application logic stay independent of the UI layer.
- The UI transition must preserve the current workflows: dashboard, clubs, sessions, shot entry, shot review, wedge matrix, analytics, imports, reports, settings, and utilities.
- WPF shell files must not remain in the shipped app once the Blazor host is in place.

---

### Task 1: Convert `CarryIQ.App` to a MAUI Blazor Hybrid host

**Files:**
- Modify: `src/CarryIQ.App/CarryIQ.App.csproj`
- Modify: `src/CarryIQ.App/AppHost.cs`
- Create: `src/CarryIQ.App/ServiceCollectionExtensions.cs`
- Create: `src/CarryIQ.App/MauiProgram.cs`
- Create: `src/CarryIQ.App/App.xaml`
- Create: `src/CarryIQ.App/App.xaml.cs`
- Create: `src/CarryIQ.App/MainPage.xaml`
- Create: `src/CarryIQ.App/MainPage.xaml.cs`
- Create: `src/CarryIQ.App/wwwroot/index.html`
- Create: `tests/CarryIQ.UnitTests/App/ServiceCollectionExtensionsTests.cs`

**Interfaces:**
- Consumes: the current `AppHost` service graph, `IApplicationPaths`, `IDatabaseInitializer`, repository registrations, and the existing view-model types.
- Produces: a MAUI startup path that loads BlazorWebView, plus a shared `AddCarryIqServices(IServiceCollection services)` extension that both the new MAUI host and the existing host wiring can reuse.

- [ ] **Step 1: Write the failing service-registration test**

Use this shape:

```csharp
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCarryIqServicesRegistersTheCoreAppGraph()
    {
        var services = new ServiceCollection();

        services.AddCarryIqServices();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IApplicationPaths>());
        Assert.NotNull(provider.GetRequiredService<IDatabaseInitializer>());
        Assert.NotNull(provider.GetRequiredService<IClubRepository>());
        Assert.NotNull(provider.GetRequiredService<MainWindowViewModel>());
    }
}
```

- [ ] **Step 2: Run the focused test and confirm it fails**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~ServiceCollectionExtensionsTests`

Expected: FAIL because `AddCarryIqServices` does not exist yet.

- [ ] **Step 3: Add the MAUI bootstrap and shared service registration**

Use this project shape:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <UseMaui>true</UseMaui>
    <UseMauiBlazorWebView>true</UseMauiBlazorWebView>
    <SingleProject>true</SingleProject>
    <RootNamespace>CarryIQ.App</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\\CarryIQ.Application\\CarryIQ.Application.csproj" />
    <ProjectReference Include="..\\CarryIQ.Infrastructure\\CarryIQ.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

Create the shared registration helper:

```csharp
namespace CarryIQ.App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCarryIqServices(this IServiceCollection services)
    {
        services.AddSingleton<IApplicationPaths, ApplicationPaths>();
        services.AddSingleton<IDatabaseConnectionFactory, DuckDbConnectionFactory>();
        services.AddSingleton<DuckDbMigrationRunner>();
        services.AddSingleton<IDatabaseInitializer, DuckDbDatabaseInitializer>();
        services.AddSingleton<IClubRepository, DuckDbClubRepository>();
        services.AddSingleton<IPracticeSessionRepository, DuckDbPracticeSessionRepository>();
        services.AddSingleton<IShotRepository, DuckDbShotRepository>();
        services.AddSingleton<IDashboardProjectionRepository, DuckDbDashboardProjectionRepository>();
        services.AddSingleton<IWedgeSwingReferenceRepository, DuckDbWedgeSwingReferenceRepository>();
        services.AddSingleton<IShotEntryPreferencesStore, JsonShotEntryPreferencesStore>();
        services.AddSingleton<IDemoDataSeeder, DemoDataSeeder>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ClubManagerViewModel>();
        services.AddSingleton<SessionManagerViewModel>();
        services.AddSingleton<ShotEntryViewModel>();
        services.AddSingleton<ShotReviewViewModel>();
        services.AddSingleton<UtilitiesViewModel>();
        services.AddSingleton<WedgeMatrixViewModel>();
        services.AddSingleton<AnalyticsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        return services;
    }
}
```

Then update `AppHost.BuildHost()` to call `AddCarryIqServices()` and add the MAUI entry point:

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts => { });

    builder.Services.AddMauiBlazorWebView();
#if DEBUG
    builder.Services.AddBlazorWebViewDeveloperTools();
#endif
    builder.Services.AddCarryIqServices();

    return builder.Build();
}
```

Use `MainPage.xaml` to host the Blazor UI:

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:blazor="clr-namespace:Microsoft.AspNetCore.Components.WebView.Maui;assembly=Microsoft.AspNetCore.Components.WebView.Maui"
             x:Class="CarryIQ.App.MainPage">
    <blazor:BlazorWebView HostPage="wwwroot/index.html">
        <blazor:BlazorWebView.RootComponents>
            <RootComponent Selector="#app" ComponentType="{x:Type local:App}" xmlns:local="clr-namespace:CarryIQ.App" />
        </blazor:BlazorWebView.RootComponents>
    </blazor:BlazorWebView>
</ContentPage>
```

- [ ] **Step 4: Run the focused test and confirm it passes**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~ServiceCollectionExtensionsTests`

Expected: PASS.

- [ ] **Step 5: Run the MAUI app project build**

Run: `dotnet build src/CarryIQ.App/CarryIQ.App.csproj`

Expected: PASS with the MAUI workload already present on this machine.

### Task 2: Add the Blazor shell and navigation layer

**Files:**
- Create: `src/CarryIQ.App/Components/_Imports.razor`
- Create: `src/CarryIQ.App/Components/App.razor`
- Create: `src/CarryIQ.App/Components/Layout/MainLayout.razor`
- Create: `src/CarryIQ.App/Components/Layout/NavRail.razor`
- Create: `src/CarryIQ.App/Components/Shell/ShellScreenHost.razor`
- Create: `src/CarryIQ.App/Components/Shell/ShellScreenComponentMap.cs`
- Create: `src/CarryIQ.App/Components/Shared/PageCard.razor`
- Create: `src/CarryIQ.App/Components/Shared/SectionHeader.razor`
- Create: `src/CarryIQ.App/Components/Shared/PlaceholderScreen.razor`
- Create: `tests/CarryIQ.UnitTests/App/ShellScreenComponentMapTests.cs`

**Interfaces:**
- Consumes: `MainWindowViewModel`, `ShellNavigationItemViewModel`, and `IShellScreenViewModel`.
- Produces: a Blazor shell that renders the current navigation items and maps each screen view-model to a Razor component.

- [ ] **Step 1: Write the failing screen-map test**

Use this shape:

```csharp
public class ShellScreenComponentMapTests
{
    [Fact]
    public void ResolveReturnsTheExpectedComponentForEachScreenViewModel()
    {
        Assert.Equal(typeof(DashboardPage), ShellScreenComponentMap.Resolve(new DashboardViewModel(...)));
        Assert.Equal(typeof(ClubManagerPage), ShellScreenComponentMap.Resolve(new ClubManagerViewModel(...)));
        Assert.Equal(typeof(SessionManagerPage), ShellScreenComponentMap.Resolve(new SessionManagerViewModel(...)));
        Assert.Equal(typeof(ShotEntryPage), ShellScreenComponentMap.Resolve(new ShotEntryViewModel(...)));
        Assert.Equal(typeof(ShotReviewPage), ShellScreenComponentMap.Resolve(new ShotReviewViewModel(...)));
        Assert.Equal(typeof(UtilitiesPage), ShellScreenComponentMap.Resolve(new UtilitiesViewModel(...)));
        Assert.Equal(typeof(WedgeMatrixPage), ShellScreenComponentMap.Resolve(new WedgeMatrixViewModel(...)));
        Assert.Equal(typeof(AnalyticsPage), ShellScreenComponentMap.Resolve(new AnalyticsViewModel(...)));
    }
}
```

- [ ] **Step 2: Run the focused test and confirm it fails**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~ShellScreenComponentMapTests`

Expected: FAIL because the Blazor screen map and components do not exist yet.

- [ ] **Step 3: Build the Blazor shell**

Create a shell layout that mirrors the current WPF shell:

```razor
<div class="shell">
    <aside class="shell-nav">
        <NavRail ViewModel="@ViewModel" />
    </aside>
    <main class="shell-content">
        <PageCard>
            <SectionHeader Title="@ViewModel.CurrentScreen?.Title" Summary="@ViewModel.CurrentScreen?.Summary" />
            <ShellScreenHost Screen="@ViewModel.CurrentScreen" />
            <p class="shell-footer">@ViewModel.CurrentScreen?.Footer</p>
        </PageCard>
    </main>
</div>
```

The shell must use `MainWindowViewModel` for the navigation items and selected screen, and `ShellScreenHost` must resolve each `IShellScreenViewModel` to the correct Razor component through `ShellScreenComponentMap`.

- [ ] **Step 4: Run the focused test and confirm it passes**

Run: `dotnet test tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj --filter FullyQualifiedName~ShellScreenComponentMapTests`

Expected: PASS.

- [ ] **Step 5: Smoke-check the shell build**

Run: `dotnet build src/CarryIQ.App/CarryIQ.App.csproj`

Expected: PASS.

### Task 3: Port the workflow screens to Razor components

**Files:**
- Create: `src/CarryIQ.App/Components/Pages/DashboardPage.razor`
- Create: `src/CarryIQ.App/Components/Pages/UtilitiesPage.razor`
- Create: `src/CarryIQ.App/Components/Pages/ClubManagerPage.razor`
- Create: `src/CarryIQ.App/Components/Pages/SessionManagerPage.razor`
- Create: `src/CarryIQ.App/Components/Pages/ShotEntryPage.razor`
- Create: `src/CarryIQ.App/Components/Pages/ShotReviewPage.razor`
- Create: `src/CarryIQ.App/Components/Pages/WedgeMatrixPage.razor`
- Create: `src/CarryIQ.App/Components/Pages/AnalyticsPage.razor`
- Modify: `src/CarryIQ.App/DashboardViewModel.cs`
- Modify: `src/CarryIQ.App/UtilitiesViewModel.cs`
- Modify: `src/CarryIQ.App/ViewModels/ClubManagerViewModel.cs`
- Modify: `src/CarryIQ.App/ViewModels/SessionManagerViewModel.cs`
- Modify: `src/CarryIQ.App/ShotEntryViewModel.cs`
- Modify: `src/CarryIQ.App/ShotReviewViewModel.cs`
- Modify: `src/CarryIQ.App/WedgeMatrixViewModel.cs`
- Modify: `src/CarryIQ.App/AnalyticsViewModel.cs`

**Interfaces:**
- Consumes: the screen view-models and the shell-component host from Task 2.
- Produces: functional Razor pages for the current workflows without any WPF user controls remaining in the app shell.

- [ ] **Step 1: Port the read-only dashboard and utilities pages first**

Implement `DashboardPage.razor` and `UtilitiesPage.razor` as the initial Blazor pages because they exercise the shared shell state, data loading, and command wiring without adding form complexity.

Example page shape:

```razor
@inject DashboardViewModel ViewModel

<div class="metric-grid">
    @foreach (var card in ViewModel.MetricCards)
    {
        <article class="metric-card">
            <h3>@card.Title</h3>
            <strong>@card.Value</strong>
            <p>@card.Description</p>
        </article>
    }
</div>
```

- [ ] **Step 2: Port the form-heavy editor screens**

Translate the current WPF forms into Razor markup for clubs, sessions, shot entry, and shot review. Preserve the existing editor view-models, command methods, validation messages, and save/cancel flows instead of duplicating business rules in the component layer.

- [ ] **Step 3: Port the analysis screens**

Translate the wedge matrix and analytics views into Razor components that bind to the existing matrix and analytics view-models and expose the same calculations and filters.

- [ ] **Step 4: Update the shell map to point every screen type at its Razor component**

When the last page is ported, `ShellScreenComponentMap` must resolve every current `IShellScreenViewModel` to a Razor component and must not fall back to WPF user controls.

- [ ] **Step 5: Run the unit and solution tests**

Run: `dotnet test CarryIQ.sln`

Expected: PASS, with only the repository's pre-existing analyzer warnings if any remain.

### Task 4: Remove WPF-specific artifacts and update docs

**Files:**
- Delete: `src/CarryIQ.App/App.xaml`
- Delete: `src/CarryIQ.App/App.xaml.cs`
- Delete: `src/CarryIQ.App/MainWindow.xaml`
- Delete: `src/CarryIQ.App/MainWindow.xaml.cs`
- Delete: `src/CarryIQ.App/Controls/ClubManagerView.xaml`
- Delete: `src/CarryIQ.App/Controls/ClubManagerView.xaml.cs`
- Delete: `src/CarryIQ.App/Controls/SessionManagerView.xaml`
- Delete: `src/CarryIQ.App/Controls/SessionManagerView.xaml.cs`
- Delete: `src/CarryIQ.App/Controls/ShotEntryView.xaml`
- Delete: `src/CarryIQ.App/Controls/ShotEntryView.xaml.cs`
- Delete: `src/CarryIQ.App/Controls/ShotReviewView.xaml`
- Delete: `src/CarryIQ.App/Controls/ShotReviewView.xaml.cs`
- Delete: `src/CarryIQ.App/Controls/DashboardView.xaml`
- Delete: `src/CarryIQ.App/Controls/DashboardView.xaml.cs`
- Delete: `src/CarryIQ.App/Controls/UtilitiesView.xaml`
- Delete: `src/CarryIQ.App/Controls/UtilitiesView.xaml.cs`
- Delete: `src/CarryIQ.App/Controls/AnalyticsView.xaml`
- Delete: `src/CarryIQ.App/Controls/AnalyticsView.xaml.cs`
- Delete: `src/CarryIQ.App/Controls/WedgeMatrixView.xaml`
- Delete: `src/CarryIQ.App/Controls/WedgeMatrixView.xaml.cs`
- Modify: `README.md`
- Modify: `docs/architecture/README.md`
- Create: `docs/decisions/0003-blazor-hybrid-desktop-shell.md`

**Interfaces:**
- Consumes: the Blazor Hybrid host, shell, and workflow pages from Tasks 1-3.
- Produces: a WPF-free shipped app, updated repository documentation, and a decision record explaining the Blazor Hybrid desktop choice.

- [ ] **Step 1: Write the decision record for the desktop host**

Document that the app now uses a MAUI Blazor Hybrid Windows host, that the current local-first DuckDB architecture is unchanged, and that the Blazor UI is the desktop shell.

- [ ] **Step 2: Update the repo documentation**

Adjust the top-level README and architecture README so they describe the Blazor Hybrid desktop host instead of the WPF shell.

- [ ] **Step 3: Remove the WPF entry points and controls**

Delete every WPF XAML entry point and control file listed above so the shipped app no longer depends on WPF shell views.

- [ ] **Step 4: Re-run the solution tests and a final build**

Run: `dotnet build CarryIQ.sln`

Run: `dotnet test CarryIQ.sln`

Expected: PASS.

- [ ] **Step 5: Commit the completed transition**

```bash
git add -A
git commit -m "feat: replace WPF shell with Blazor hybrid"
```
