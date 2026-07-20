using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CarryIQ.App;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private ShellNavigationItemViewModel? selectedNavigationItem;
    private readonly ClubManagerViewModel? _clubManager;
    private readonly ShotEntryViewModel? _shotEntry;

    public MainWindowViewModel(
        IApplicationPaths applicationPaths,
        ClubManagerViewModel? clubManager = null,
        ShotEntryViewModel? shotEntry = null)
    {
        _clubManager = clubManager;
        _shotEntry = shotEntry;
        ApplicationTitle = "CarryIQ";
        Subtitle = "Local golf analysis foundation";
        DatabasePath = applicationPaths.DatabasePath;

        NavigationItems =
        [
            CreateNavigationItem(
                "Dashboard",
                "Overview",
                "A quick read on the latest practice sessions and carry summaries.",
                new PlaceholderScreenViewModel(
                    "Dashboard",
                    "A quick read on the latest practice sessions and carry summaries.",
                    [
                        "Show the most recent session activity at a glance.",
                        "Surface the key carry and consistency signals first.",
                        "Reserve space for future alerts and shortcuts.",
                    ],
                    "The dashboard will become the first landing page for everyday use.")),
            CreateNavigationItem(
                "Sessions",
                "Session history",
                "Browse and compare practice sessions without leaving the shell.",
                new PlaceholderScreenViewModel(
                    "Sessions",
                    "Browse and compare practice sessions without leaving the shell.",
                    [
                        "List recent sessions with date, location, and sample counts.",
                        "Open one session to review its shots and calculated summaries.",
                        "Support keyboard-first browsing through the history list.",
                    ],
                    "This view will anchor the session review workflow.")),
            CreateNavigationItem(
                "Shot Entry",
                "Manual capture",
                "Enter shots directly when a session needs precise manual input.",
                shotEntry is null
                    ? new PlaceholderScreenViewModel(
                        "Shot Entry",
                        "Enter shots directly when a session needs precise manual input.",
                        [
                            "Capture club, carry, speed, and notes in one place.",
                            "Keep the form clean enough for quick, repetitive entry.",
                            "Leave room for validation feedback and import parity later.",
                        ],
                        "This screen will support the manual shot workflow.")
                    : shotEntry),
            CreateNavigationItem(
                "Imports",
                "Bring data in",
                "Import compatible files into the local database.",
                new PlaceholderScreenViewModel(
                    "Imports",
                    "Import compatible files into the local database.",
                    [
                        "Separate importer choices from the rest of the shell.",
                        "Show file status and results in a predictable location.",
                        "Keep the path for future drag-and-drop or batch imports open.",
                    ],
                    "Import flows will live here when file handling is added.")),
            CreateNavigationItem(
                "Club Gapping",
                "Distance gaps",
                "Compare clubs and spot distance gaps that need attention.",
                new PlaceholderScreenViewModel(
                    "Club Gapping",
                    "Compare clubs and spot distance gaps that need attention.",
                    [
                        "Highlight carry gaps by club and shaft configuration.",
                        "Make it easy to compare adjacent clubs side by side.",
                        "Reserve room for charting or tables later in the release.",
                    ],
                    "This page will evolve into the gapping analysis workspace.")),
            CreateNavigationItem(
                "Wedge Matrix",
                "Short game",
                "Track wedge distances and shot patterns across lofts and swings.",
                new PlaceholderScreenViewModel(
                    "Wedge Matrix",
                    "Track wedge distances and shot patterns across lofts and swings.",
                    [
                        "Organize wedge data by loft, swing length, and carry band.",
                        "Support repeatable short-game comparisons.",
                        "Leave space for a matrix-style layout in a later pass.",
                    ],
                    "This is the placeholder for wedge matrix analysis.")),
            CreateNavigationItem(
                "Dispersion",
                "Shot pattern",
                "Review shot spread and target control for each club or session.",
                new PlaceholderScreenViewModel(
                    "Dispersion",
                    "Review shot spread and target control for each club or session.",
                    [
                        "Show the relationship between strike pattern and consistency.",
                        "Keep a path open for scatter plots and zone summaries.",
                        "Make the future target-line analysis easy to find.",
                    ],
                    "Dispersion reporting will live behind this entry.")),
            CreateNavigationItem(
                "Trends",
                "Performance over time",
                "Track how carry, consistency, and speed change over time.",
                new PlaceholderScreenViewModel(
                    "Trends",
                    "Track how carry, consistency, and speed change over time.",
                    [
                        "Surface month-over-month changes without leaving the shell.",
                        "Keep the future trend summaries visible from the main nav.",
                        "Reserve the page for time-series charts and filters.",
                    ],
                    "This section will host longitudinal performance views.")),
            CreateNavigationItem(
                "Clubs",
                "Equipment setup",
                "Maintain the club list that drives the rest of the app.",
                clubManager is null
                    ? new PlaceholderScreenViewModel(
                        "Clubs",
                        "Maintain the club list that drives the rest of the app.",
                        [
                            "Keep the equipment list close to the analysis pages.",
                            "Make club naming and ordering obvious in the shell.",
                            "Leave room for editing loft, shaft, and bag composition.",
                        ],
                        "Club management starts here.")
                    : clubManager),
            CreateNavigationItem(
                "Reports",
                "Shareable outputs",
                "Prepare summaries and exports for later reporting work.",
                new PlaceholderScreenViewModel(
                    "Reports",
                    "Prepare summaries and exports for later reporting work.",
                    [
                        "Keep report generation separate from day-to-day analysis.",
                        "Leave room for printable and exportable outputs.",
                        "Make future report templates easy to introduce.",
                    ],
                    "The reporting hub will live behind this placeholder.")),
            CreateNavigationItem(
                "Settings",
                "Application options",
                "Manage local preferences and application-level choices.",
                new PlaceholderScreenViewModel(
                    "Settings",
                    "Manage local preferences and application-level choices.",
                    [
                        "Surface data locations and app preferences in one place.",
                        "Make future configuration options easy to discover.",
                        "Keep the route stable as the shell expands.",
                    ],
                    "Settings will eventually hold app configuration.")),
        ];

        SelectedNavigationItem = NavigationItems[0];
    }

    public string ApplicationTitle { get; }

    public string Subtitle { get; }

    public string DatabasePath { get; }

    public ObservableCollection<ShellNavigationItemViewModel> NavigationItems { get; }

    public ShellNavigationItemViewModel? SelectedNavigationItem
    {
        get => selectedNavigationItem;
        set
        {
            if (SetProperty(ref selectedNavigationItem, value))
            {
                OnPropertyChanged(nameof(CurrentScreen));
            }
        }
    }

    public IShellScreenViewModel? CurrentScreen => SelectedNavigationItem?.Screen;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_clubManager is not null)
        {
            await _clubManager.InitializeAsync(cancellationToken);
        }

        if (_shotEntry is not null)
        {
            await _shotEntry.InitializeAsync(cancellationToken);
        }
    }

    private static ShellNavigationItemViewModel CreateNavigationItem(
        string title,
        string eyebrow,
        string summary,
        IShellScreenViewModel screen)
    {
        return new ShellNavigationItemViewModel(title, eyebrow, summary, screen);
    }
}
