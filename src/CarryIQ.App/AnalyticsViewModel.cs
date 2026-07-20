using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarryIQ.App;

public sealed partial class AnalyticsViewModel : ObservableObject, IShellScreenViewModel, IDisposable
{
    private readonly IClubRepository _clubRepository;
    private readonly IShotRepository _shotRepository;
    private readonly ObservableCollection<AnalyticsClubRowViewModel> _rows = [];
    private readonly ObservableCollection<SelectionOption> _gapOptions = [];
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    private AnalyticsClubRowViewModel? _selectedRow;
    private ClubGapOption _selectedGapOption = ClubGapOption.Median;
    private string? _statusMessage;
    private string? _errorMessage;
    private bool _isInitialized;

    public AnalyticsViewModel(
        IClubRepository clubRepository,
        IShotRepository shotRepository)
    {
        _clubRepository = clubRepository;
        _shotRepository = shotRepository;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);

        GapOptions.Add(new SelectionOption("Median gaps", ClubGapOption.Median));
        GapOptions.Add(new SelectionOption("Mean gaps", ClubGapOption.Mean));
    }

    public ObservableCollection<AnalyticsClubRowViewModel> Rows => _rows;

    public ObservableCollection<SelectionOption> GapOptions => _gapOptions;

    public AnalyticsClubRowViewModel? SelectedRow
    {
        get => _selectedRow;
        set => SetProperty(ref _selectedRow, value);
    }

    public ClubGapOption SelectedGapOption
    {
        get => _selectedGapOption;
        set
        {
            if (SetProperty(ref _selectedGapOption, value))
            {
                if (_isInitialized)
                {
                    _ = RefreshAsync();
                }
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

    public string Title { get; } = "Club Gapping";

    public string Summary { get; } = "Analyse included shots to spot carry consistency, overlap, and weak gaps.";

    public string Footer { get; } = "Included shots are analysed by default. Switch between median and mean gaps to compare spacing.";

    public IAsyncRelayCommand RefreshCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);
        _isInitialized = true;
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

            var clubs = await _clubRepository.SearchAsync(new ClubSearchCriteria(ActiveOnly: true), cancellationToken);
            var shots = await _shotRepository.SearchAsync(new ShotSearchCriteria(IncludedOnly: true), cancellationToken);

            var carriedShotsByClub = shots
                .Where(shot => shot.CarryDistance is not null)
                .GroupBy(shot => shot.ClubId)
                .ToDictionary(group => group.Key, group => group.Select(shot => shot.CarryDistance!.Value).ToArray());

            var analytics = ClubAnalyticsCalculator.Calculate(
                clubs.Select(club => (
                    club.Id,
                    club.Name,
                    carriedShotsByClub.TryGetValue(club.Id, out var carries)
                        ? carries.AsEnumerable()
                        : Enumerable.Empty<Distance>())),
                SelectedGapOption);

            _rows.Clear();
            var clubById = clubs.ToDictionary(club => club.Id);

            foreach (var clubAnalytics in analytics.Clubs)
            {
                if (clubAnalytics.ClubId is not Guid clubId || !clubById.TryGetValue(clubId, out var club))
                {
                    continue;
                }

                var row = new AnalyticsClubRowViewModel(club, clubAnalytics);
                _rows.Add(row);
            }

            for (var index = 0; index < analytics.Gaps.Count; index++)
            {
                var gap = analytics.Gaps[index];
                var lower = _rows.FirstOrDefault(row => row.ClubName == gap.LowerClubName);
                if (lower is null)
                {
                    continue;
                }

                lower.GapToNextYards = gap.GapYards;
                lower.HasOverlap = gap.HasOverlap;
                lower.HasGapWarning = gap.HasWarning;
            }

            if (_rows.Count == 0)
            {
                SelectedRow = null;
                StatusMessage = "No active clubs with included shots were found.";
                return;
            }

            if (SelectedRow is null || !_rows.Any(row => row.ClubId == SelectedRow.ClubId))
            {
                SelectedRow = _rows[0];
            }

            StatusMessage = string.Format(
                CultureInfo.InvariantCulture,
                "Loaded {0} club rows from {1} included shots.",
                _rows.Count,
                analytics.Clubs.Sum(row => row.Statistics.SampleCount));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Unable to load analytics.";
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    public void Dispose()
    {
        _refreshGate.Dispose();
    }
}
