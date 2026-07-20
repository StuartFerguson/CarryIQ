# CarryIQ

CarryIQ is a Windows desktop golf analysis application focused on reliable carry-distance data, club gapping, consistency, wedge matrices, and local-only practice analysis.

## Screenshots

Placeholder for future UI screenshots.

## Features

- Local DuckDB storage
- WPF desktop shell
- Manual shot entry foundation
- Generic and SwingLogic import architecture
- Club, session, and shot domain model
- Club gapping and consistency calculations
- Backup and restore foundation

## Requirements

- Windows 10 or later
- .NET 10
- WPF
- x64

## Build

```powershell
dotnet restore
dotnet build CarryIQ.sln -c Release
```

## Test

```powershell
dotnet test CarryIQ.sln -c Release
```

## Data location

User data is stored under the current user's LocalApplicationData folder.

## Backup

Backups are local files and include the DuckDB database plus application settings.

## Import

The first release supports generic CSV files and SwingLogic SLX exports through isolated importer interfaces.

## Architecture

- `CarryIQ.Domain` contains entities, enums, value objects, and analytics models.
- `CarryIQ.Application` contains use cases, repository/importer contracts, and storage-path abstractions.
- `CarryIQ.Infrastructure` contains DuckDB persistence, path adapters, and bootstrap logic.
- `CarryIQ.App` contains the WPF shell and dependency-injection startup.

## Roadmap

v0.1 focuses on the foundation, local persistence, manual entry, import, analytics, and export basics. Later releases add richer reports, more importers, and deeper trend analysis.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).
