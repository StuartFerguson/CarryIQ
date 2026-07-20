using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarryIQ.App;

public sealed partial class UtilitiesViewModel : ObservableObject, IShellScreenViewModel
{
    private readonly IDemoDataSeeder _demoDataSeeder;
    private readonly DashboardViewModel? _dashboard;
    private readonly SessionManagerViewModel? _sessionManager;
    private readonly ShotReviewViewModel? _shotReview;
    private readonly ClubManagerViewModel? _clubManager;
    private readonly AnalyticsViewModel? _analytics;
    private readonly WedgeMatrixViewModel? _wedgeMatrix;
    private bool _isSeeding;
    private string _sessionCountText = "20";
    private string _weekSpanText = "4";
    private string? _statusMessage;
    private string? _errorMessage;

    public UtilitiesViewModel(
        IDemoDataSeeder demoDataSeeder,
        DashboardViewModel? dashboard = null,
        SessionManagerViewModel? sessionManager = null,
        ShotReviewViewModel? shotReview = null,
        ClubManagerViewModel? clubManager = null,
        AnalyticsViewModel? analytics = null,
        WedgeMatrixViewModel? wedgeMatrix = null)
    {
        _demoDataSeeder = demoDataSeeder;
        _dashboard = dashboard;
        _sessionManager = sessionManager;
        _shotReview = shotReview;
        _clubManager = clubManager;
        _analytics = analytics;
        _wedgeMatrix = wedgeMatrix;

        SeedDemoDataCommand = new AsyncRelayCommand(SeedDemoDataAsync, CanSeedDemoData);
    }

    public string Title { get; } = "Utilities";

    public string Summary { get; } = "Local helper tools for generating demo data and other maintenance tasks.";

    public string Footer { get; } = "Use this hub for test data generation and future maintenance utilities.";

    public string SessionCountText
    {
        get => _sessionCountText;
        set
        {
            if (SetProperty(ref _sessionCountText, value))
            {
                ErrorMessage = null;
                SeedDemoDataCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string WeekSpanText
    {
        get => _weekSpanText;
        set
        {
            if (SetProperty(ref _weekSpanText, value))
            {
                ErrorMessage = null;
                SeedDemoDataCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public IAsyncRelayCommand SeedDemoDataCommand { get; }

    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private bool CanSeedDemoData() => !_isSeeding;

    private async Task SeedDemoDataAsync(CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(SessionCountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sessionCount))
        {
            ErrorMessage = "Enter a valid session count.";
            return;
        }

        if (!int.TryParse(WeekSpanText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var weekSpan))
        {
            ErrorMessage = "Enter a valid week span.";
            return;
        }

        _isSeeding = true;
        SeedDemoDataCommand.NotifyCanExecuteChanged();

        try
        {
            ErrorMessage = null;
            StatusMessage = "Seeding demo data...";

            var result = await _demoDataSeeder.SeedAsync(new DemoDataSeedOptions(sessionCount, weekSpan), cancellationToken);
            StatusMessage = string.Format(
                CultureInfo.InvariantCulture,
                "Seeded {0} sessions and {1} shots over {2} week{3}.",
                result.SessionCount,
                result.ShotCount,
                weekSpan,
                weekSpan == 1 ? string.Empty : "s");

            if (result.CreatedClubCount > 0)
            {
                StatusMessage += string.Format(
                    CultureInfo.InvariantCulture,
                    " Created {0} club{1} because the bag was empty.",
                    result.CreatedClubCount,
                    result.CreatedClubCount == 1 ? string.Empty : "s");
            }

            await RefreshScreensAsync(cancellationToken);
            StatusMessage += " Dashboard and session views refreshed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Unable to seed demo data.";
        }
        finally
        {
            _isSeeding = false;
            SeedDemoDataCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task RefreshScreensAsync(CancellationToken cancellationToken)
    {
        if (_clubManager is not null)
        {
            await _clubManager.InitializeAsync(cancellationToken);
        }

        if (_sessionManager is not null)
        {
            await _sessionManager.InitializeAsync(cancellationToken);
        }

        if (_shotReview is not null)
        {
            await _shotReview.InitializeAsync(cancellationToken);
        }

        if (_dashboard is not null)
        {
            await _dashboard.InitializeAsync(cancellationToken);
        }

        if (_analytics is not null)
        {
            await _analytics.InitializeAsync(cancellationToken);
        }

        if (_wedgeMatrix is not null)
        {
            await _wedgeMatrix.InitializeAsync(cancellationToken);
        }
    }
}
