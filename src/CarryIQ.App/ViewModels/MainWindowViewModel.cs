namespace CarryIQ.App;

public sealed class MainWindowViewModel(IApplicationPaths applicationPaths, ClubManagerViewModel clubManager)
{
    public string Title { get; } = "CarryIQ";

    public string Subtitle { get; } = "Local golf analysis foundation";

    public string DatabasePath => applicationPaths.DatabasePath;

    public string StatusMessage { get; } = "Manage the club bag, preserve historic shot links, and keep the default starter set editable.";

    public ClubManagerViewModel ClubManager { get; } = clubManager;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await ClubManager.InitializeAsync(cancellationToken);
    }
}
