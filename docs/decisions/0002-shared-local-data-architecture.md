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
