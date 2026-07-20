using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarryIQ.App;

public sealed partial class DashboardViewModel : ObservableObject, IShellScreenViewModel, IDisposable
{
    private readonly IDashboardProjectionRepository _projectionRepository;
    private readonly IDatabaseConnectionFactory? _connectionFactory;
    private readonly ObservableCollection<DashboardMetricCardViewModel> _metricCards = [];
    private readonly ObservableCollection<RecentSessionRowViewModel> _recentSessions = [];
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly Guid? _explicitGolferProfileId;
    private readonly DominantHand? _explicitDominantHand;
    private Guid _golferProfileId;
    private DominantHand _dominantHand = DominantHand.Right;
    private bool _isInitialized;
    private RecentSessionRowViewModel? _selectedSession;
    private string? _statusMessage;
    private string? _errorMessage;

    public DashboardViewModel(
        IDashboardProjectionRepository projectionRepository,
        Guid? golferProfileId = null,
        DominantHand? dominantHand = null,
        IDatabaseConnectionFactory? connectionFactory = null)
    {
        _projectionRepository = projectionRepository;
        _connectionFactory = connectionFactory;
        _explicitGolferProfileId = golferProfileId;
        _explicitDominantHand = dominantHand;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, CanRefresh);
    }

    public string Title { get; } = "Dashboard";

    public string Summary { get; } = "A performance-first summary of carry, consistency, bias, and recent practice sessions.";

    public string Footer { get; } = "Start here to see the latest performance metrics, then drill into the most recent sessions below.";

    public ObservableCollection<DashboardMetricCardViewModel> MetricCards => _metricCards;

    public ObservableCollection<RecentSessionRowViewModel> RecentSessions => _recentSessions;

    public RecentSessionRowViewModel? SelectedSession
    {
        get => _selectedSession;
        set => SetProperty(ref _selectedSession, value);
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

    public IAsyncRelayCommand RefreshCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_explicitGolferProfileId is Guid golferProfileId)
        {
            _golferProfileId = golferProfileId;
        }
        else
        {
            if (_connectionFactory is null)
            {
                throw new InvalidOperationException("A database connection factory is required when no golfer profile id is supplied.");
            }

            _golferProfileId = await LoadDefaultGolferProfileIdAsync(cancellationToken);
        }

        _dominantHand = _explicitDominantHand ?? await LoadDominantHandAsync(cancellationToken);
        await RefreshAsync(cancellationToken);
        _isInitialized = true;
        RefreshCommand.NotifyCanExecuteChanged();
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!await _refreshGate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            ErrorMessage = null;

            var source = await _projectionRepository.LoadAsync(_golferProfileId, 8, cancellationToken);
            var projection = DashboardProjectionCalculator.Calculate(
                source.Shots,
                source.RecentSessions,
                _dominantHand,
                8);

            var selectedSessionId = SelectedSession?.SessionId;
            BuildMetricCards(projection);
            BuildRecentSessions(projection);
            RestoreSelection(selectedSessionId);
            StatusMessage = string.Format(
                CultureInfo.InvariantCulture,
                "Loaded {0} shots across {1} recent session{2}.",
                projection.Metrics.SampleSize,
                RecentSessions.Count,
                RecentSessions.Count == 1 ? string.Empty : "s");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Unable to load dashboard metrics.";
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private void BuildMetricCards(DashboardProjection projection)
    {
        MetricCards.Clear();
        MetricCards.Add(new DashboardMetricCardViewModel("Average carry", FormatYards(projection.Metrics.AverageCarryYards), "Included shots only"));
        MetricCards.Add(new DashboardMetricCardViewModel("Carry consistency", FormatYards(projection.Metrics.CarryStandardDeviationYards), "Population standard deviation"));
        MetricCards.Add(new DashboardMetricCardViewModel("Left/right bias", FormatSignedYards(projection.Metrics.LeftRightBiasYards), "Positive is target-right for right-handed golfers"));
        MetricCards.Add(new DashboardMetricCardViewModel("Long/short bias", FormatSignedYards(projection.Metrics.LongShortBiasYards), "Carry minus target distance"));
        MetricCards.Add(new DashboardMetricCardViewModel("Offline spread", FormatYards(projection.Metrics.OfflineSpreadYards), "Average absolute offline"));
        MetricCards.Add(new DashboardMetricCardViewModel("Sample size", projection.Metrics.SampleSize.ToString(CultureInfo.InvariantCulture), "Included shots used in calculations"));
    }

    private void BuildRecentSessions(DashboardProjection projection)
    {
        RecentSessions.Clear();
        foreach (var session in projection.RecentSessions)
        {
            RecentSessions.Add(new RecentSessionRowViewModel(
                session.SessionId,
                session.SessionDate,
                session.Name,
                session.TotalShots,
                session.IncludedShotCount,
                session.AverageCarryYards));
        }
    }

    private void RestoreSelection(Guid? previousSessionId)
    {
        if (previousSessionId is Guid selectedId)
        {
            SelectedSession = RecentSessions.FirstOrDefault(session => session.SessionId == selectedId) ?? RecentSessions.FirstOrDefault();
        }
        else
        {
            SelectedSession = RecentSessions.FirstOrDefault();
        }
    }

    private static string FormatYards(decimal value) => $"{value:0.#} yd";

    private static string FormatSignedYards(decimal value) => $"{value:+0.#;-0.#;0} yd";

    private bool CanRefresh() => _isInitialized;

    private async Task<Guid> LoadDefaultGolferProfileIdAsync(CancellationToken cancellationToken)
    {
        if (_connectionFactory is null)
        {
            return Guid.Empty;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM GolferProfiles ORDER BY CreatedAt LIMIT 1;";

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value switch
        {
            Guid guid => guid,
            null => Guid.Empty,
            _ => Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!),
        };
    }

    private async Task<DominantHand> LoadDominantHandAsync(CancellationToken cancellationToken)
    {
        if (_connectionFactory is null || _golferProfileId == Guid.Empty)
        {
            return DominantHand.Right;
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DominantHand
            FROM GolferProfiles
            WHERE Id = $golferProfileId;
            """;
        DuckDbPersistenceHelpers.AddParameter(command, "$golferProfileId", _golferProfileId);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value switch
        {
            int dominantHand => (DominantHand)dominantHand,
            long dominantHand => (DominantHand)(int)dominantHand,
            string dominantHand => Enum.Parse<DominantHand>(dominantHand, ignoreCase: true),
            _ => DominantHand.Right,
        };
    }

    public void Dispose()
    {
        _refreshGate.Dispose();
    }
}
