using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarryIQ.App;

public sealed class ShotEntryViewModel : ObservableObject, IShellScreenViewModel, INotifyDataErrorInfo
{
    private readonly IClubRepository _clubRepository;
    private readonly IPracticeSessionRepository _sessionRepository;
    private readonly IShotRepository _shotRepository;
    private readonly IShotEntryPreferencesStore _preferencesStore;
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    private Guid? _selectedClubId;
    private Guid? _selectedSessionId;
    private string _carryDistanceText = string.Empty;
    private string _totalDistanceText = string.Empty;
    private string _ballSpeedText = string.Empty;
    private string _clubSpeedText = string.Empty;
    private string _launchAngleText = string.Empty;
    private string _launchDirectionText = string.Empty;
    private string? _notes;
    private string? _statusMessage;
    private bool _isInitialized;
    private bool _isSaving;

    public ShotEntryViewModel(
        IClubRepository clubRepository,
        IPracticeSessionRepository sessionRepository,
        IShotRepository shotRepository,
        IShotEntryPreferencesStore preferencesStore)
    {
        _clubRepository = clubRepository;
        _sessionRepository = sessionRepository;
        _shotRepository = shotRepository;
        _preferencesStore = preferencesStore;
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
    }

    public ObservableCollection<ClubSummary> Clubs { get; } = [];

    public ObservableCollection<PracticeSessionSummary> Sessions { get; } = [];

    public string Title { get; } = "Shot Entry";

    public string Summary { get; } = "Capture a shot quickly with keyboard-first inputs and remembered club selection.";

    public string Footer { get; } = "Press Enter to save the shot and keep the club selection ready for the next entry.";

    public Guid? SelectedClubId
    {
        get => _selectedClubId;
        set
        {
            if (SetProperty(ref _selectedClubId, value))
            {
                ClearErrors(nameof(SelectedClubId));
                NotifyCommandStateChanged();
            }
        }
    }

    public Guid? SelectedSessionId
    {
        get => _selectedSessionId;
        set
        {
            if (SetProperty(ref _selectedSessionId, value))
            {
                ClearErrors(nameof(SelectedSessionId));
                NotifyCommandStateChanged();
            }
        }
    }

    public string CarryDistanceText
    {
        get => _carryDistanceText;
        set => SetNumericText(ref _carryDistanceText, value, nameof(CarryDistanceText), required: true, allowNegative: false);
    }

    public string TotalDistanceText
    {
        get => _totalDistanceText;
        set => SetNumericText(ref _totalDistanceText, value, nameof(TotalDistanceText), required: false, allowNegative: false);
    }

    public string BallSpeedText
    {
        get => _ballSpeedText;
        set => SetNumericText(ref _ballSpeedText, value, nameof(BallSpeedText), required: false, allowNegative: false);
    }

    public string ClubSpeedText
    {
        get => _clubSpeedText;
        set => SetNumericText(ref _clubSpeedText, value, nameof(ClubSpeedText), required: false, allowNegative: false);
    }

    public string LaunchAngleText
    {
        get => _launchAngleText;
        set => SetNumericText(ref _launchAngleText, value, nameof(LaunchAngleText), required: false, allowNegative: true);
    }

    public string LaunchDirectionText
    {
        get => _launchDirectionText;
        set => SetNumericText(ref _launchDirectionText, value, nameof(LaunchDirectionText), required: false, allowNegative: true);
    }

    public string? Notes
    {
        get => _notes;
        set
        {
            if (SetProperty(ref _notes, value))
            {
                NotifyCommandStateChanged();
            }
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasErrors => _errors.Count > 0;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IAsyncRelayCommand SaveCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var clubs = await _clubRepository.SearchAsync(new ClubSearchCriteria(ActiveOnly: true), cancellationToken);
        var sessions = await _sessionRepository.SearchAsync(new SessionSearchCriteria(), cancellationToken);
        var preferences = await _preferencesStore.LoadAsync(cancellationToken);

        Clubs.Clear();
        foreach (var club in clubs.OrderBy(club => club.SortOrder).ThenBy(club => club.Name))
        {
            Clubs.Add(club);
        }

        Sessions.Clear();
        foreach (var session in sessions.OrderByDescending(session => session.SessionDate).ThenBy(session => session.Name))
        {
            Sessions.Add(session);
        }

        SelectedSessionId = Sessions.FirstOrDefault()?.Id;
        SelectedClubId = preferences.LastClubId is Guid lastClubId && Clubs.Any(club => club.Id == lastClubId)
            ? lastClubId
            : Clubs.FirstOrDefault()?.Id;

        _isInitialized = true;
        NotifyCommandStateChanged();
    }

    public IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName is null)
        {
            return Array.Empty<string>();
        }

        return _errors.TryGetValue(propertyName, out var errors) ? errors : Array.Empty<string>();
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (_isSaving)
        {
            return;
        }

        _isSaving = true;
        NotifyCommandStateChanged();
        try
        {
            ValidateAll();
            if (HasErrors || SelectedClubId is not Guid clubId || SelectedSessionId is not Guid sessionId)
            {
                return;
            }

            var nextSequence = await LoadNextShotSequenceAsync(sessionId, cancellationToken);
            var shot = CreateShot(clubId, sessionId, nextSequence);
            await _shotRepository.AddAsync(shot, cancellationToken);
            await _preferencesStore.SaveAsync(new ShotEntryPreferences { LastClubId = clubId }, cancellationToken);

            ClearEntryFields();
            StatusMessage = "Shot saved.";
        }
        finally
        {
            _isSaving = false;
            NotifyCommandStateChanged();
        }
    }

    private async Task<int> LoadNextShotSequenceAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var shots = await _shotRepository.SearchAsync(new ShotSearchCriteria(PracticeSessionId: sessionId), cancellationToken);
        return shots.Count == 0 ? 1 : shots.Max(shot => shot.ShotSequence) + 1;
    }

    private bool CanSave() =>
        _isInitialized &&
        !_isSaving &&
        SelectedClubId.HasValue &&
        SelectedSessionId.HasValue &&
        !HasErrors;

    private Shot CreateShot(Guid clubId, Guid sessionId, int shotSequence) =>
        new()
        {
            Id = Guid.NewGuid(),
            PracticeSessionId = sessionId,
            ClubId = clubId,
            ShotSequence = shotSequence,
            RecordedAt = DateTimeOffset.UtcNow,
            Source = ShotSourceKind.Manual,
            CarryDistance = Distance.FromYards(decimal.Parse(CarryDistanceText, CultureInfo.CurrentCulture)),
            TotalDistance = string.IsNullOrWhiteSpace(TotalDistanceText) ? null : Distance.FromYards(decimal.Parse(TotalDistanceText, CultureInfo.CurrentCulture)),
            BallSpeed = string.IsNullOrWhiteSpace(BallSpeedText) ? null : Speed.FromMilesPerHour(decimal.Parse(BallSpeedText, CultureInfo.CurrentCulture)),
            ClubSpeed = string.IsNullOrWhiteSpace(ClubSpeedText) ? null : Speed.FromMilesPerHour(decimal.Parse(ClubSpeedText, CultureInfo.CurrentCulture)),
            LaunchAngle = string.IsNullOrWhiteSpace(LaunchAngleText) ? null : decimal.Parse(LaunchAngleText, CultureInfo.CurrentCulture),
            LaunchDirection = string.IsNullOrWhiteSpace(LaunchDirectionText) ? null : decimal.Parse(LaunchDirectionText, CultureInfo.CurrentCulture),
            IsIncluded = true,
            ExclusionReason = null,
            IsEstimated = false,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            RawImportData = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    private void ClearEntryFields()
    {
        CarryDistanceText = string.Empty;
        TotalDistanceText = string.Empty;
        BallSpeedText = string.Empty;
        ClubSpeedText = string.Empty;
        LaunchAngleText = string.Empty;
        LaunchDirectionText = string.Empty;
        Notes = null;
        StatusMessage = null;
    }

    private void ValidateAll()
    {
        ValidateNumericField(CarryDistanceText, nameof(CarryDistanceText), required: true, allowNegative: false);
        ValidateNumericField(TotalDistanceText, nameof(TotalDistanceText), required: false, allowNegative: false);
        ValidateNumericField(BallSpeedText, nameof(BallSpeedText), required: false, allowNegative: false);
        ValidateNumericField(ClubSpeedText, nameof(ClubSpeedText), required: false, allowNegative: false);
        ValidateNumericField(LaunchAngleText, nameof(LaunchAngleText), required: false, allowNegative: true);
        ValidateNumericField(LaunchDirectionText, nameof(LaunchDirectionText), required: false, allowNegative: true);
        ValidateSelection(SelectedClubId, nameof(SelectedClubId), "Choose a club.");
        ValidateSelection(SelectedSessionId, nameof(SelectedSessionId), "Choose a practice session.");
    }

    private void SetNumericText(ref string field, string value, string propertyName, bool required, bool allowNegative)
    {
        if (SetProperty(ref field, value))
        {
            ValidateNumericField(value, propertyName, required, allowNegative);
            NotifyCommandStateChanged();
        }
    }

    private void ValidateNumericField(string value, string propertyName, bool required, bool allowNegative)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                SetErrors(propertyName, [$"{FormatPropertyName(propertyName)} is required."]);
            }
            else
            {
                ClearErrors(propertyName);
            }

            return;
        }

        if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsed))
        {
            SetErrors(propertyName, [$"{FormatPropertyName(propertyName)} must be a number."]);
            return;
        }

        if (!allowNegative && parsed < 0)
        {
            SetErrors(propertyName, [$"{FormatPropertyName(propertyName)} must be zero or greater."]);
            return;
        }

        ClearErrors(propertyName);
    }

    private void ValidateSelection(Guid? value, string propertyName, string message)
    {
        if (value.HasValue)
        {
            ClearErrors(propertyName);
            return;
        }

        SetErrors(propertyName, [message]);
    }

    private void SetErrors(string propertyName, IReadOnlyCollection<string> errors)
    {
        var nextErrors = errors.ToList();
        if (_errors.TryGetValue(propertyName, out var existing) && existing.SequenceEqual(nextErrors))
        {
            return;
        }

        _errors[propertyName] = nextErrors;
        OnErrorsChanged(propertyName);
    }

    private void ClearErrors(string propertyName)
    {
        if (_errors.Remove(propertyName))
        {
            OnErrorsChanged(propertyName);
        }
    }

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        NotifyCommandStateChanged();
    }

    private void NotifyCommandStateChanged()
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    private static string FormatPropertyName(string propertyName) =>
        propertyName switch
        {
            nameof(CarryDistanceText) => "Carry distance",
            nameof(TotalDistanceText) => "Total distance",
            nameof(BallSpeedText) => "Ball speed",
            nameof(ClubSpeedText) => "Club speed",
            nameof(LaunchAngleText) => "Launch angle",
            nameof(LaunchDirectionText) => "Launch direction",
            _ => propertyName,
        };
}
