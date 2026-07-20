using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CarryIQ.App;

public sealed partial class WedgeMatrixViewModel : ObservableObject, IShellScreenViewModel, IDisposable
{
    private readonly IClubRepository _clubRepository;
    private readonly IWedgeSwingReferenceRepository _referenceRepository;
    private readonly IDatabaseConnectionFactory? _connectionFactory;
    private readonly ObservableCollection<WedgeMatrixRowViewModel> _rows = [];
    private readonly Dictionary<Guid, WedgeMatrixRow> _rowSources = [];
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    private readonly Guid? _explicitGolferProfileId;
    private IReadOnlyList<WedgeSwingReference> _references = [];
    private Guid _golferProfileId;
    private WedgeMatrixRowViewModel? _selectedRow;
    private WedgeMatrixRowEditorViewModel? _selectedRowEditor;
    private string? _statusMessage;
    private string? _errorMessage;
    private bool _includeInactive;
    private bool _isInitialized;
    private bool _isSaving;

    public WedgeMatrixViewModel(
        IClubRepository clubRepository,
        IWedgeSwingReferenceRepository referenceRepository,
        IDatabaseConnectionFactory? connectionFactory = null,
        Guid? golferProfileId = null)
    {
        _clubRepository = clubRepository;
        _referenceRepository = referenceRepository;
        _connectionFactory = connectionFactory;
        _explicitGolferProfileId = golferProfileId;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
    }

    public ObservableCollection<WedgeMatrixRowViewModel> Rows => _rows;

    public WedgeMatrixRowViewModel? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (SetProperty(ref _selectedRow, value))
            {
                UpdateSelectedRowEditor();
                NotifyCommandStateChanged();
            }
        }
    }

    public WedgeMatrixRowEditorViewModel? SelectedRowEditor
    {
        get => _selectedRowEditor;
        private set
        {
            if (SetProperty(ref _selectedRowEditor, value))
            {
                NotifyCommandStateChanged();
            }
        }
    }

    public bool IncludeInactive
    {
        get => _includeInactive;
        set
        {
            if (SetProperty(ref _includeInactive, value) && _isInitialized)
            {
                _ = RefreshAsync();
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

    public string Title { get; } = "Wedge Matrix";

    public string Summary { get; } = "Review the wedge reference matrix and edit A1, A2, and A3 overrides from the selected row.";

    public string Footer { get; } = "Select a wedge in the grid above to edit or replace the detailed A1, A2, and A3 references below.";

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

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

        await RefreshAsync(cancellationToken);
        _isInitialized = true;
        NotifyCommandStateChanged();
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

            if (_golferProfileId == Guid.Empty)
            {
                StatusMessage = "No golfer profile was available.";
                _rows.Clear();
                _rowSources.Clear();
                SelectedRow = null;
                return;
            }

            var clubs = await _clubRepository.SearchAsync(
                new ClubSearchCriteria(_golferProfileId, ActiveOnly: IncludeInactive ? null : true),
                cancellationToken);
            var references = await _referenceRepository.SearchAsync(_golferProfileId, cancellationToken);
            _references = references;

            var matrix = WedgeMatrixCalculator.Calculate(
                clubs.Select(club => new WedgeMatrixClub
                {
                    Id = club.Id,
                    Name = club.Name,
                    ClubType = club.ClubType,
                    SortOrder = club.SortOrder,
                    IsActive = club.IsActive,
                }),
                references,
                IncludeInactive);

            var selectedClubId = SelectedRow?.ClubId;
            _rows.Clear();
            _rowSources.Clear();
            foreach (var row in matrix.Rows)
            {
                _rowSources[row.Club.Id] = row;
                _rows.Add(new WedgeMatrixRowViewModel(row));
            }

            SelectedRow = selectedClubId is Guid clubId
                ? _rows.FirstOrDefault(row => row.ClubId == clubId) ?? _rows.FirstOrDefault()
                : _rows.FirstOrDefault();

            var referenceCellCount = matrix.Rows.Sum(CountCells);
            StatusMessage = string.Format(
                CultureInfo.InvariantCulture,
                "Loaded {0} wedge club{1} with {2} reference cell{3}.",
                _rows.Count,
                _rows.Count == 1 ? string.Empty : "s",
                referenceCellCount,
                referenceCellCount == 1 ? string.Empty : "s");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Unable to load wedge matrix.";
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (_isSaving || SelectedRowEditor is null)
        {
            return;
        }

        _isSaving = true;
        NotifyCommandStateChanged();

        try
        {
            ErrorMessage = null;

            var references = SelectedRowEditor.ToReferences(_golferProfileId);
            foreach (var reference in references)
            {
                await _referenceRepository.SaveAsync(reference, cancellationToken);
            }

            await RefreshAsync(cancellationToken);
            StatusMessage = "Wedge references saved.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Unable to save wedge references.";
        }
        finally
        {
            _isSaving = false;
            NotifyCommandStateChanged();
        }
    }

    private static int CountCells(WedgeMatrixRow row) =>
        new[] { row.A1, row.A2, row.A3 }.Count(cell => cell is not null);

    private bool CanSave() => _isInitialized && !_isSaving && SelectedRowEditor is not null;

    private void UpdateSelectedRowEditor()
    {
        if (SelectedRow is null || !_rowSources.TryGetValue(SelectedRow.ClubId, out var sourceRow))
        {
            SelectedRowEditor = null;
            return;
        }

        var editor = new WedgeMatrixRowEditorViewModel(
            sourceRow.Club.Id,
            sourceRow.Club.Name,
            sourceRow.Club.ClubType,
            sourceRow.Club.IsActive);
        editor.Load(_references.Where(reference => reference.ClubId == sourceRow.Club.Id).ToList());
        SelectedRowEditor = editor;
    }

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

    private void NotifyCommandStateChanged()
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _refreshGate.Dispose();
    }
}
