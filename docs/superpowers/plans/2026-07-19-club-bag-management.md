# Club Bag Management Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver club CRUD, activation, ordering, duplicate-name validation, and a WPF club manager screen without breaking historic shots.

**Architecture:** Keep the club rules in the application and repository layers, with soft-delete semantics so shots can continue to reference inactive clubs. Add one club-management view model to the WPF shell so the UI stays thin and the repository remains the persistence boundary.

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, DuckDB.NET, xUnit v3.

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

### Task 1: Club repository contract and tests

**Files:**
- Modify: `src/CarryIQ.Application/Abstractions/IClubRepository.cs`
- Modify: `src/CarryIQ.Application/Models/ClubSearchCriteria.cs`
- Modify: `src/CarryIQ.Application/Models/ClubSummary.cs`
- Create: `tests/CarryIQ.UnitTests/Domain/ClubValidationTests.cs`
- Create: `tests/CarryIQ.IntegrationTests/Persistence/DuckDbClubRepositoryTests.cs`

**Interfaces:**
- Consumes: `Club`, `ClubSearchCriteria`, `ClubSummary`
- Produces: repository behavior for get/search/save/delete, plus duplicate-name and active-order expectations

- [ ] **Step 1: Write the failing unit and integration tests**
- [ ] **Step 2: Run the tests and confirm they fail for missing club persistence behavior**
- [ ] **Step 3: Update the contracts and test helpers to express active/inactive and sort-order rules**
- [ ] **Step 4: Re-run the tests and confirm the new contract shape is now exercised**

### Task 2: DuckDB club persistence

**Files:**
- Create: `src/CarryIQ.Infrastructure/Persistence/DuckDbClubRepository.cs`
- Modify: `src/CarryIQ.Infrastructure/CarryIQ.Infrastructure.csproj`
- Modify: `src/CarryIQ.App/AppHost.cs`
- Modify: `src/CarryIQ.Infrastructure/Bootstrap/DuckDbDatabaseInitializer.cs`

**Interfaces:**
- Consumes: `IClubRepository`, `IDatabaseConnectionFactory`, `Club`
- Produces: real CRUD persistence backed by `Clubs`, plus default starter-bag ordering and soft delete behavior

- [ ] **Step 1: Write the minimal repository implementation to satisfy the red tests**
- [ ] **Step 2: Wire the repository into dependency injection**
- [ ] **Step 3: Re-run the club repository tests and bootstrap tests**
- [ ] **Step 4: Fix any schema/bootstrap mismatches revealed by the tests**

### Task 3: WPF club manager screen

**Files:**
- Create: `src/CarryIQ.App/ViewModels/ClubManagerViewModel.cs`
- Create: `src/CarryIQ.App/ViewModels/ClubEditorViewModel.cs`
- Create: `src/CarryIQ.App/Controls/ClubManagerView.xaml`
- Create: `src/CarryIQ.App/Controls/ClubManagerView.xaml.cs`
- Modify: `src/CarryIQ.App/MainWindowViewModel.cs`
- Modify: `src/CarryIQ.App/MainWindow.xaml`

**Interfaces:**
- Consumes: `IClubRepository`, `ClubSummary`
- Produces: add/edit/deactivate/reorder UI bound to the repository-backed club list

- [ ] **Step 1: Write the view-model tests for create/edit/deactivate/reorder commands**
- [ ] **Step 2: Implement the view models and bindable club editor**
- [ ] **Step 3: Add the WPF view to the main window**
- [ ] **Step 4: Verify the app still starts and the club screen renders**

### Task 4: Verification pass

**Files:**
- Modify: `README.md` if needed

**Interfaces:**
- Consumes: built app and test projects
- Produces: verified solution state for the club-management slice

- [ ] **Step 1: Run the unit and integration test suites**
- [ ] **Step 2: Run a Release build of the solution**
- [ ] **Step 3: Fix any remaining compile or test issues**
- [ ] **Step 4: Capture the final working state**
