namespace CarryIQ.Infrastructure;

public sealed class ApplicationPaths : IApplicationPaths
{
    public ApplicationPaths()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DataDirectory = Path.Combine(root, "CarryIQ");
        DatabasePath = Path.Combine(DataDirectory, "carryiq.duckdb");
        SettingsPath = Path.Combine(DataDirectory, "user-settings.json");
        LogsDirectory = Path.Combine(DataDirectory, "logs");
        BackupsDirectory = Path.Combine(DataDirectory, "backups");
    }

    public string DataDirectory { get; }

    public string DatabasePath { get; }

    public string SettingsPath { get; }

    public string LogsDirectory { get; }

    public string BackupsDirectory { get; }
}
