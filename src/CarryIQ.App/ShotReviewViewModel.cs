using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarryIQ.App;

public sealed partial class ShotReviewViewModel : ObservableObject, IShellScreenViewModel
{
    private readonly IClubRepository _clubRepository;
    private readonly IPracticeSessionRepository _sessionRepository;
    private readonly IShotRepository _shotRepository;
    private readonly Dictionary<Guid, string> _clubNames = new();
    private readonly List<Shot> _allShots = [];
    private Guid? _selectedSessionId;
    private ShotReviewRowViewModel? _selectedShot;
    private Guid? _selectedClubId;
    private SwingType? _selectedSwingType;
    private string _searchText = string.Empty;
    private bool _includedOnly;
    private string? _statusMessage;
    private bool _isInitialized;
    private bool _isRefreshing;

    public ShotReviewViewModel(
        IClubRepository clubRepository,
        IPracticeSessionRepository sessionRepository,
        IShotRepository shotRepository)
    {
        _clubRepository = clubRepository;
        _sessionRepository = sessionRepository;
        _shotRepository = shotRepository;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, CanRefresh);
        IncludeSelectedCommand = new AsyncRelayCommand(() => UpdateSelectedShotsAsync(shot => shot with
        {
            IsIncluded = true,
            ExclusionReason = null,
            UpdatedAt = DateTimeOffset.UtcNow,
        }), CanUpdateSelectedShots);
        ExcludeSelectedCommand = new AsyncRelayCommand(() => UpdateSelectedShotsAsync(shot => shot with
        {
            IsIncluded = false,
            ExclusionReason = "Manual review",
            UpdatedAt = DateTimeOffset.UtcNow,
        }), CanUpdateSelectedShots);
        ApplyClubCommand = new AsyncRelayCommand(ApplyClubAsync, CanUpdateSelectedShots);
        ApplySwingTypeCommand = new AsyncRelayCommand(ApplySwingTypeAsync, CanUpdateSelectedShots);
    }

    public IReadOnlyList<SelectionOption> SwingTypeOptions { get; } =
    [
        new SelectionOption("Keep existing", null),
        new SelectionOption("Full", SwingType.Full),
        new SelectionOption("Three quarter", SwingType.ThreeQuarter),
        new SelectionOption("Half", SwingType.Half),
        new SelectionOption("Quarter", SwingType.Quarter),
        new SelectionOption("Pitch", SwingType.Pitch),
        new SelectionOption("Chip", SwingType.Chip),
        new SelectionOption("Punch", SwingType.Punch),
        new SelectionOption("Other", SwingType.Other),
    ];

    public ObservableCollection<PracticeSessionSummary> Sessions { get; } = [];

    public ObservableCollection<ClubSummary> Clubs { get; } = [];

    public ObservableCollection<ShotReviewRowViewModel> Shots { get; } = [];

    public string Title { get; } = "Shot Review";

    public string Summary { get; } = "Search recent shots, inspect raw data, and bulk-correct the selected rows.";

    public string Footer { get; } = "Use the session picker and search box to narrow the list, then select rows for bulk include, exclude, or edit actions.";

    public Guid? SelectedSessionId
    {
        get => _selectedSessionId;
        set
        {
            if (SetProperty(ref _selectedSessionId, value))
            {
                NotifyCommandsChanged();
            }
        }
    }

    public ShotReviewRowViewModel? SelectedShot
    {
        get => _selectedShot;
        set
        {
            if (SetProperty(ref _selectedShot, value))
            {
                if (value is not null)
                {
                    if (SelectedClubId is null)
                    {
                        SelectedClubId = value.ClubId;
                    }

                    if (SelectedSwingType is null)
                    {
                        SelectedSwingType = value.SwingType;
                    }
                }

                NotifyCommandsChanged();
            }
        }
    }

    public Guid? SelectedClubId
    {
        get => _selectedClubId;
        set
        {
            if (SetProperty(ref _selectedClubId, value))
            {
                NotifyCommandsChanged();
            }
        }
    }

    public SwingType? SelectedSwingType
    {
        get => _selectedSwingType;
        set
        {
            if (SetProperty(ref _selectedSwingType, value))
            {
                NotifyCommandsChanged();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                NotifyCommandsChanged();
            }
        }
    }

    public bool IncludedOnly
    {
        get => _includedOnly;
        set
        {
            if (SetProperty(ref _includedOnly, value))
            {
                NotifyCommandsChanged();
            }
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand IncludeSelectedCommand { get; }

    public IAsyncRelayCommand ExcludeSelectedCommand { get; }

    public IAsyncRelayCommand ApplyClubCommand { get; }

    public IAsyncRelayCommand ApplySwingTypeCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await LoadSupportDataAsync(cancellationToken);
        SelectedSessionId = Sessions.OrderByDescending(session => session.SessionDate).FirstOrDefault()?.Id;
        _isInitialized = true;
        await RefreshAsync(cancellationToken);
    }

    private async Task LoadSupportDataAsync(CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.SearchAsync(new SessionSearchCriteria(), cancellationToken);
        var clubs = await _clubRepository.SearchAsync(new ClubSearchCriteria(ActiveOnly: true), cancellationToken);

        Sessions.Clear();
        foreach (var session in sessions.OrderByDescending(session => session.SessionDate).ThenBy(session => session.Name))
        {
            Sessions.Add(session);
        }

        Clubs.Clear();
        _clubNames.Clear();
        foreach (var club in clubs.OrderBy(club => club.SortOrder).ThenBy(club => club.Name))
        {
            Clubs.Add(club);
            _clubNames[club.Id] = club.Name;
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        NotifyCommandsChanged();
        try
        {
            if (SelectedSessionId is null)
            {
                Shots.Clear();
                SelectedShot = null;
                StatusMessage = "Choose a session to review.";
                return;
            }

            var shots = await _shotRepository.SearchAsync(
                new ShotSearchCriteria(PracticeSessionId: SelectedSessionId, IncludedOnly: IncludedOnly ? true : null),
                cancellationToken);

            _allShots.Clear();
            _allShots.AddRange(shots.OrderByDescending(shot => shot.RecordedAt).ThenByDescending(shot => shot.ShotSequence));
            RebuildVisibleShots();
            StatusMessage = $"{Shots.Count} shots loaded.";
        }
        finally
        {
            _isRefreshing = false;
            NotifyCommandsChanged();
        }
    }

    private void RebuildVisibleShots()
    {
        var previousSelectedId = SelectedShot?.Id;
        var visibleShots = _allShots.Where(ShotMatchesFilters).ToList();

        Shots.Clear();
        foreach (var shot in visibleShots)
        {
            Shots.Add(new ShotReviewRowViewModel(shot, GetClubName(shot.ClubId)));
        }

        if (previousSelectedId is Guid selectedId)
        {
            SelectedShot = Shots.FirstOrDefault(shot => shot.Id == selectedId) ?? Shots.FirstOrDefault();
        }
        else
        {
            SelectedShot = Shots.FirstOrDefault();
        }
    }

    private bool ShotMatchesFilters(Shot shot)
    {
        if (IncludedOnly && !shot.IsIncluded)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim().ToLowerInvariant();
            var clubName = GetClubName(shot.ClubId).ToLowerInvariant();
            var notes = (shot.Notes ?? string.Empty).ToLowerInvariant();
            var exclusionReason = (shot.ExclusionReason ?? string.Empty).ToLowerInvariant();
            var raw = (shot.RawImportData ?? string.Empty).ToLowerInvariant();
            if (!clubName.Contains(search) && !notes.Contains(search) && !exclusionReason.Contains(search) && !raw.Contains(search))
            {
                return false;
            }
        }

        return true;
    }

    private string GetClubName(Guid clubId) => _clubNames.TryGetValue(clubId, out var clubName) ? clubName : "Unknown club";

    private async Task UpdateSelectedShotsAsync(Func<Shot, Shot> updater)
    {
        var targetRows = GetTargetRows();
        if (targetRows.Count == 0)
        {
            return;
        }

        foreach (var row in targetRows)
        {
            var shot = _allShots.FirstOrDefault(item => item.Id == row.Id);
            if (shot is null)
            {
                continue;
            }

            var updated = updater(shot with
            {
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            await _shotRepository.UpdateAsync(updated, CancellationToken.None);

            var index = _allShots.FindIndex(item => item.Id == updated.Id);
            if (index >= 0)
            {
                _allShots[index] = updated;
            }
        }

        await RefreshAsync();
    }

    private async Task ApplyClubAsync()
    {
        if (SelectedClubId is not Guid clubId)
        {
            return;
        }

        await UpdateSelectedShotsAsync(shot => shot with
        {
            ClubId = clubId,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
    }

    private async Task ApplySwingTypeAsync()
    {
        await UpdateSelectedShotsAsync(shot => shot with
        {
            SwingType = SelectedSwingType,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
    }

    private List<ShotReviewRowViewModel> GetTargetRows()
    {
        var selected = Shots.Where(shot => shot.IsSelected).ToList();
        return selected.Count > 0 ? selected : (SelectedShot is null ? [] : [SelectedShot]);
    }

    private bool CanRefresh() => _isInitialized && !_isRefreshing;

    private bool CanUpdateSelectedShots() => _isInitialized && GetTargetRows().Count > 0;

    private void NotifyCommandsChanged()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        IncludeSelectedCommand.NotifyCanExecuteChanged();
        ExcludeSelectedCommand.NotifyCanExecuteChanged();
        ApplyClubCommand.NotifyCanExecuteChanged();
        ApplySwingTypeCommand.NotifyCanExecuteChanged();
    }
}
