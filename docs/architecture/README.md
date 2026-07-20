# Architecture

CarryIQ uses a modular monolith split into domain, application, infrastructure, and WPF presentation projects.

## Shared Local Data

CarryIQ keeps one DuckDB database per user profile in the platform app-data root. The host chooses the root; the application layer defines the repository contracts; infrastructure owns the DuckDB implementation. This keeps the storage model consistent across WPF, MAUI, and future Blazor desktop hosts.
