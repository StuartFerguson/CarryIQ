# CarryIQ Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create the repository foundation for CarryIQ: solution structure, build/CI scaffolding, core domain model, and DuckDB initialization with tests.

**Architecture:** Start with a modular monolith that separates domain, application contracts, infrastructure, and WPF presentation. Keep the first increment focused on the pieces required to bootstrap future feature work: stable project layout, shared package/version management, deterministic persistence initialization, and a small but representative domain model that can support clubs, sessions, and shots.

**Tech Stack:** .NET 10, WPF, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Configuration, Microsoft.Extensions.Logging, DuckDB.NET, xUnit, FluentAssertions, Serilog, System.Text.Json, GitHub Actions.

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

### Task 1: Repository and build scaffold

**Files:**
- Create: `CarryIQ.sln`
- Create: `Directory.Build.props`
- Create: `Directory.Packages.props`
- Create: `.editorconfig`
- Create: `.gitignore`
- Create: `.gitattributes`
- Create: `README.md`
- Create: `LICENSE`
- Create: `SECURITY.md`
- Create: `CONTRIBUTING.md`
- Create: `CODE_OF_CONDUCT.md`
- Create: `.github/workflows/ci.yml`
- Create: `.github/pull_request_template.md`
- Create: `.github/ISSUE_TEMPLATE/bug_report.md`
- Create: `.github/ISSUE_TEMPLATE/feature_request.md`

**Produces:**
- A solution that restores and builds on Windows.
- Central package versions and shared build settings for all projects.

- [ ] **Step 1: Add repo metadata and build defaults**
- [ ] **Step 2: Add CI workflow that restores, builds, tests, and uploads artifacts**
- [ ] **Step 3: Add repo documentation stubs with v0.1 scope**
- [ ] **Step 4: Verify solution-level restore/build once projects exist**

### Task 2: Project structure and application host

**Files:**
- Create: `src/CarryIQ.Domain/CarryIQ.Domain.csproj`
- Create: `src/CarryIQ.Application/CarryIQ.Application.csproj`
- Create: `src/CarryIQ.Infrastructure/CarryIQ.Infrastructure.csproj`
- Create: `src/CarryIQ.App/CarryIQ.App.csproj`
- Create: `tests/CarryIQ.UnitTests/CarryIQ.UnitTests.csproj`
- Create: `tests/CarryIQ.IntegrationTests/CarryIQ.IntegrationTests.csproj`
- Create: `src/CarryIQ.App/App.xaml`
- Create: `src/CarryIQ.App/App.xaml.cs`
- Create: `src/CarryIQ.App/AppHost.cs`
- Create: `src/CarryIQ.App/MainWindow.xaml`
- Create: `src/CarryIQ.App/MainWindow.xaml.cs`

**Produces:**
- A minimal WPF shell wired to dependency injection and hosting.
- Test projects linked to the solution and ready for the domain/infrastructure work.

- [ ] **Step 1: Create project files and solution references**
- [ ] **Step 2: Wire the WPF app to a generic host**
- [ ] **Step 3: Add a minimal main window and startup path**
- [ ] **Step 4: Verify the app builds in Release configuration**

### Task 3: Domain model foundation

**Files:**
- Create: `src/CarryIQ.Domain/Enums/*.cs`
- Create: `src/CarryIQ.Domain/ValueObjects/*.cs`
- Create: `src/CarryIQ.Domain/Entities/*.cs`
- Create: `src/CarryIQ.Domain/Analytics/*.cs`
- Create: `tests/CarryIQ.UnitTests/Domain/*.cs`

**Produces:**
- Core entities, enums, and calculation models for golfer profiles, practice sessions, clubs, shots, wedge references, and analytics summaries.

- [ ] **Step 1: Write tests for enums, units, and value-object invariants**
- [ ] **Step 2: Implement the immutable domain types and rules**
- [ ] **Step 3: Add tests for consistency/statistics models**
- [ ] **Step 4: Verify all domain tests pass**

### Task 4: DuckDB initialization and persistence contracts

**Files:**
- Create: `src/CarryIQ.Application/Abstractions/*.cs`
- Create: `src/CarryIQ.Infrastructure/Persistence/*.cs`
- Create: `src/CarryIQ.Infrastructure/Migrations/*.sql`
- Create: `src/CarryIQ.Infrastructure/Bootstrap/*.cs`
- Create: `tests/CarryIQ.IntegrationTests/Persistence/*.cs`

**Produces:**
- Database bootstrap, schema versioning, default golfer and bag creation, and isolated integration tests that validate first-run initialization.

- [ ] **Step 1: Write failing integration tests for first-run bootstrap**
- [ ] **Step 2: Implement schema creation and version tracking**
- [ ] **Step 3: Implement default profile/bag seeding**
- [ ] **Step 4: Verify bootstrap tests pass against isolated databases**

### Task 5: Documentation and sample data placeholders for v0.1

**Files:**
- Create: `docs/architecture/README.md`
- Create: `docs/decisions/0001-modular-monolith.md`
- Create: `docs/user-guide/README.md`
- Create: `docs/import-formats/README.md`
- Create: `tests/CarryIQ.IntegrationTests/TestData/*.csv`

**Produces:**
- Minimal documentation structure and representative fixtures to support later import work.

- [ ] **Step 1: Add architecture and decision records**
- [ ] **Step 2: Add sample fixture files for future import tests**
- [ ] **Step 3: Verify documentation paths exist and are referenced from the README**

### Task 6: Initial verification pass

**Files:**
- Modify: `README.md`
- Modify: `.github/workflows/ci.yml`

**Produces:**
- A repo that passes local restore/build/test and is ready for the first vertical feature slice.

- [ ] **Step 1: Run restore/build/test locally**
- [ ] **Step 2: Fix any package or project wiring issues**
- [ ] **Step 3: Confirm CI workflow mirrors local commands**

