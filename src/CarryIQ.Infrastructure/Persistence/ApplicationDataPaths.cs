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
