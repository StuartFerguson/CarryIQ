using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarryIQ.App;

public sealed class SessionManagerViewModel : ObservableObject, IShellScreenViewModel
{
    private readonly IPracticeSessionRepository _sessionRepository;
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly PracticeSessionEditorViewModel _editor = new();
    private readonly ObservableCollection<PracticeSessionSummary> _sessions = [];
    private readonly ObservableCollection<SelectionOption> _golferProfiles = [];
    private readonly ObservableCollection<SelectionOption> _sessionTypeOptions = [];
    private readonly ObservableCollection<SelectionOption> _archiveOptions = [];

    private PracticeSessionSummary? _selectedSession;
    private Guid? _selectedGolferProfileId;
    private DateOnly? _startDate;
    private DateOnly? _endDate;
    private SessionType? _selectedSessionType;
    private string? _launchMonitorSourceFilter;
    private bool? _selectedArchived;
    private string? _searchText;
    private string? _errorMessage;
    private Guid _defaultGolferProfileId;

    public SessionManagerViewModel(
        IPracticeSessionRepository sessionRepository,
        IDatabaseConnectionFactory connectionFactory)
    {
        _sessionRepository = sessionRepository;
        _connectionFactory = connectionFactory;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        NewCommand = new AsyncRelayCommand(BeginNewSessionAsync);
        SaveCommand = new AsyncRelayCommand(SaveCurrentSessionAsync);
        DuplicateCommand = new AsyncRelayCommand(DuplicateCurrentSessionAsync, CanActOnSelectedSession);
        ArchiveCommand = new AsyncRelayCommand(ToggleArchiveAsync, CanActOnSelectedSession);
        DeleteCommand = new AsyncRelayCommand(DeleteCurrentSessionAsync, CanActOnSelectedSession);

        _editor.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
            {
                ErrorMessage = null;
            }

            if (e.PropertyName is nameof(PracticeSessionEditorViewModel.IsArchived))
            {
                OnPropertyChanged(nameof(ArchiveButtonText));
            }
        };

        SessionTypeOptions.Add(new SelectionOption("All types", null));
        foreach (var sessionType in Enum.GetValues<SessionType>())
        {
            SessionTypeOptions.Add(new SelectionOption(sessionType.ToString(), sessionType));
        }

        ArchiveOptions.Add(new SelectionOption("All sessions", null));
        ArchiveOptions.Add(new SelectionOption("Active only", false));
        ArchiveOptions.Add(new SelectionOption("Archived only", true));
    }

    public ObservableCollection<PracticeSessionSummary> Sessions => _sessions;

    public ObservableCollection<SelectionOption> GolferProfiles => _golferProfiles;

    public ObservableCollection<SelectionOption> SessionTypeOptions => _sessionTypeOptions;

    public ObservableCollection<SelectionOption> ArchiveOptions => _archiveOptions;

    public PracticeSessionEditorViewModel Editor => _editor;

    public string Title { get; } = "Sessions";

    public string Summary { get; } = "Browse, edit, duplicate, archive, and delete practice sessions.";

    public string Footer { get; } = "The session history now uses a master-detail grid layout.";

    public PracticeSessionSummary? SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (SetProperty(ref _selectedSession, value))
            {
                DuplicateCommand.NotifyCanExecuteChanged();
                ArchiveCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public Guid? SelectedGolferProfileId
    {
        get => _selectedGolferProfileId;
        set => SetProperty(ref _selectedGolferProfileId, value);
    }

    public DateOnly? StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateOnly? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public SessionType? SelectedSessionType
    {
        get => _selectedSessionType;
        set => SetProperty(ref _selectedSessionType, value);
    }

    public string? LaunchMonitorSourceFilter
    {
        get => _launchMonitorSourceFilter;
        set => SetProperty(ref _launchMonitorSourceFilter, value);
    }

    public bool? SelectedArchived
    {
        get => _selectedArchived;
        set => SetProperty(ref _selectedArchived, value);
    }

    public string? SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string ArchiveButtonText => Editor.IsArchived ? "Restore" : "Archive";

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand NewCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand DuplicateCommand { get; }

    public IAsyncRelayCommand ArchiveCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await LoadGolferProfilesAsync(cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    public async Task SelectSessionAsync(Guid? sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId is null)
        {
            await BeginNewSessionAsync(cancellationToken);
            return;
        }

        var session = await _sessionRepository.GetAsync(sessionId.Value, cancellationToken);
        if (session is null)
        {
            await BeginNewSessionAsync(cancellationToken);
            return;
        }

        SelectedSession = _sessions.FirstOrDefault(item => item.Id == sessionId.Value);
        Editor.Load(session);
        ErrorMessage = null;
        OnPropertyChanged(nameof(ArchiveButtonText));
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_golferProfiles.Count == 0)
        {
            await LoadGolferProfilesAsync(cancellationToken);
        }

        var results = await _sessionRepository.SearchAsync(
            new SessionSearchCriteria(
                SelectedGolferProfileId,
                StartDate,
                EndDate,
                SelectedSessionType,
                LaunchMonitorSourceFilter,
                SelectedArchived,
                SearchText),
            cancellationToken);

        _sessions.Clear();
        foreach (var session in results)
        {
            _sessions.Add(session);
        }

        if (_sessions.Count == 0)
        {
            await BeginNewSessionAsync(cancellationToken);
            return;
        }

        var selectedId = SelectedSession?.Id ?? _sessions[0].Id;
        await SelectSessionAsync(selectedId, cancellationToken);
    }

    private async Task BeginNewSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_defaultGolferProfileId == Guid.Empty)
        {
            await LoadGolferProfilesAsync(cancellationToken);
        }

        var golferProfileId = SelectedGolferProfileId ?? _defaultGolferProfileId;
        Editor.StartNew(golferProfileId);
        SelectedSession = null;
        ErrorMessage = null;
        OnPropertyChanged(nameof(ArchiveButtonText));
    }

    private async Task SaveCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = Editor.ToPracticeSession();
        var errors = SessionRules.Validate(session);
        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, errors);
            return;
        }

        try
        {
            await _sessionRepository.SaveAsync(session, cancellationToken);
            await RefreshAsync(cancellationToken);
            await SelectSessionAsync(session.Id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task DuplicateCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedSession is null)
        {
            return;
        }

        var current = await _sessionRepository.GetAsync(SelectedSession.Id, cancellationToken);
        if (current is null)
        {
            return;
        }

        Editor.DuplicateFrom(current);
        SelectedSession = null;
        await SaveCurrentSessionAsync(cancellationToken);
    }

    private async Task ToggleArchiveAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedSession is null)
        {
            return;
        }

        Editor.IsArchived = !Editor.IsArchived;
        await SaveCurrentSessionAsync(cancellationToken);
    }

    private async Task DeleteCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedSession is null)
        {
            return;
        }

        await _sessionRepository.DeleteAsync(SelectedSession.Id, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    private bool CanActOnSelectedSession() => SelectedSession is not null;

    private async Task LoadGolferProfilesAsync(CancellationToken cancellationToken)
    {
        _golferProfiles.Clear();
        _golferProfiles.Add(new SelectionOption("All golfers", null));

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, DisplayName
            FROM GolferProfiles
            ORDER BY CreatedAt;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = Guid.Parse(Convert.ToString(reader["Id"], CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
            var displayName = Convert.ToString(reader["DisplayName"], CultureInfo.InvariantCulture) ?? "Golfer";
            _golferProfiles.Add(new SelectionOption(displayName, id));
        }

        _defaultGolferProfileId = _golferProfiles.Skip(1).Select(option => (Guid?)option.Value).FirstOrDefault() ?? Guid.Empty;
        if (SelectedGolferProfileId is null && _defaultGolferProfileId != Guid.Empty)
        {
            SelectedGolferProfileId = _defaultGolferProfileId;
        }
    }
}
