namespace CarryIQ.App;

public sealed class MainWindowViewModel(IApplicationPaths applicationPaths)
{
    public string Title { get; } = "CarryIQ";

    public string Subtitle { get; } = "Local golf analysis foundation";

    public string DatabasePath => applicationPaths.DatabasePath;

    public string StatusMessage { get; } = "The local database is ready and the shell is wired for the first release.";
}
