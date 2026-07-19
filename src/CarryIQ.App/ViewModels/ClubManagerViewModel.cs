using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarryIQ.App;

public sealed class ClubManagerViewModel : ObservableObject, IShellScreenViewModel
{
    private readonly IClubRepository _clubRepository;
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ObservableCollection<ClubSummary> _clubs = [];
    private readonly ClubEditorViewModel _editor = new();

    private ClubSummary? _selectedClub;
    private Guid _golferProfileId;
    private string? _errorMessage;

    public ClubManagerViewModel(
        IClubRepository clubRepository,
        IDatabaseConnectionFactory connectionFactory)
    {
        _clubRepository = clubRepository;
        _connectionFactory = connectionFactory;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        NewCommand = new AsyncRelayCommand(BeginNewClubAsync);
        var saveCommand = new AsyncRelayCommand(SaveCurrentClubAsync, CanSaveCurrentClub);
        SaveCommand = saveCommand;
        DeactivateCommand = new AsyncRelayCommand(DeactivateSelectedClubAsync);
        MoveUpCommand = new AsyncRelayCommand(MoveSelectedClubUpAsync);
        MoveDownCommand = new AsyncRelayCommand(MoveSelectedClubDownAsync);

        _editor.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ClubEditorViewModel.ValidationMessage) or nameof(ClubEditorViewModel.HasValidationErrors))
            {
                saveCommand.NotifyCanExecuteChanged();
            }

            if (e.PropertyName is not null)
            {
                ErrorMessage = null;
            }
        };
    }

    public ObservableCollection<ClubSummary> Clubs => _clubs;

    public ClubEditorViewModel Editor => _editor;

    public string Title { get; } = "Clubs";

    public string Summary { get; } = "Maintain the club bag, ordering, activation, and validation.";

    public string Footer { get; } = "Club management starts here.";

    public ClubSummary? SelectedClub
    {
        get => _selectedClub;
        set => SetProperty(ref _selectedClub, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand NewCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand DeactivateCommand { get; }

    public IAsyncRelayCommand MoveUpCommand { get; }

    public IAsyncRelayCommand MoveDownCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _golferProfileId = await LoadDefaultGolferProfileIdAsync(cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    public async Task SelectClubAsync(Guid? clubId, CancellationToken cancellationToken = default)
    {
        if (clubId is null)
        {
            await BeginNewClubAsync(cancellationToken);
            return;
        }

        var club = await _clubRepository.GetAsync(clubId.Value, cancellationToken);
        if (club is null)
        {
            await BeginNewClubAsync(cancellationToken);
            return;
        }

        SelectedClub = _clubs.FirstOrDefault(item => item.Id == clubId.Value);
        Editor.Load(club);
        ErrorMessage = null;
    }

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_golferProfileId == Guid.Empty)
        {
            _golferProfileId = await LoadDefaultGolferProfileIdAsync(cancellationToken);
        }

        var clubs = await _clubRepository.SearchAsync(
            new ClubSearchCriteria(_golferProfileId, ActiveOnly: true),
            cancellationToken);

        _clubs.Clear();
        foreach (var club in clubs)
        {
            _clubs.Add(club);
        }

        if (_clubs.Count == 0)
        {
            await BeginNewClubAsync(cancellationToken);
            return;
        }

        var selectedId = SelectedClub?.Id ?? _clubs[0].Id;
        await SelectClubAsync(selectedId, cancellationToken);
    }

    private async Task BeginNewClubAsync(CancellationToken cancellationToken = default)
    {
        if (_golferProfileId == Guid.Empty)
        {
            _golferProfileId = await LoadDefaultGolferProfileIdAsync(cancellationToken);
        }

        var allClubs = await _clubRepository.SearchAsync(
            new ClubSearchCriteria(_golferProfileId),
            cancellationToken);
        var nextSortOrder = allClubs.Count == 0 ? 0 : allClubs.Max(club => club.SortOrder) + 1;

        SelectedClub = null;
        Editor.StartNew(_golferProfileId, nextSortOrder);
        ErrorMessage = null;
    }

    private async Task SaveCurrentClubAsync(CancellationToken cancellationToken = default)
    {
        var validationErrors = ClubRules.Validate(Editor.ToClub());
        if (validationErrors.Count > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, validationErrors);
            return;
        }

        try
        {
            var club = Editor.ToClub();
            await _clubRepository.SaveAsync(club, cancellationToken);
            await RefreshAsync(cancellationToken);
            await SelectClubAsync(club.Id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private bool CanSaveCurrentClub() => !Editor.HasValidationErrors;

    private async Task DeactivateSelectedClubAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedClub is null)
        {
            return;
        }

        await _clubRepository.DeleteAsync(SelectedClub.Id, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    private async Task MoveSelectedClubUpAsync(CancellationToken cancellationToken = default)
    {
        if (!await MoveSelectedClubAsync(-1, cancellationToken))
        {
            return;
        }
    }

    private async Task MoveSelectedClubDownAsync(CancellationToken cancellationToken = default)
    {
        if (!await MoveSelectedClubAsync(+1, cancellationToken))
        {
            return;
        }
    }

    private async Task<bool> MoveSelectedClubAsync(int offset, CancellationToken cancellationToken)
    {
        if (SelectedClub is null)
        {
            return false;
        }

        var index = _clubs.IndexOf(SelectedClub);
        var targetIndex = index + offset;
        if (index < 0 || targetIndex < 0 || targetIndex >= _clubs.Count)
        {
            return false;
        }

        var current = await _clubRepository.GetAsync(SelectedClub.Id, cancellationToken);
        var target = await _clubRepository.GetAsync(_clubs[targetIndex].Id, cancellationToken);
        if (current is null || target is null)
        {
            return false;
        }

        (current, target) = (current with { SortOrder = target.SortOrder, UpdatedAt = DateTimeOffset.UtcNow },
                             target with { SortOrder = current.SortOrder, UpdatedAt = DateTimeOffset.UtcNow });

        await _clubRepository.SaveAsync(target, cancellationToken);
        await _clubRepository.SaveAsync(current, cancellationToken);
        await RefreshAsync(cancellationToken);
        await SelectClubAsync(current.Id, cancellationToken);
        return true;
    }

    private async Task<Guid> LoadDefaultGolferProfileIdAsync(CancellationToken cancellationToken)
    {
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
}
