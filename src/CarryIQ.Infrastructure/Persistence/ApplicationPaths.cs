namespace CarryIQ.Infrastructure;

public sealed class ApplicationPaths : IApplicationPaths
{
    private readonly ApplicationDataPaths _layout;

    public ApplicationPaths()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _layout = ApplicationDataPaths.Create(root);
    }

    public string DataDirectory => _layout.DataDirectory;

    public string DatabasePath => _layout.DatabasePath;

    public string SettingsPath => _layout.SettingsPath;

    public string LogsDirectory => _layout.LogsDirectory;

    public string BackupsDirectory => _layout.BackupsDirectory;
}
