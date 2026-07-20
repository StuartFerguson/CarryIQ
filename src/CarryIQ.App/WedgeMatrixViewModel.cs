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
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    private readonly Guid? _explicitGolferProfileId;
    private Guid _golferProfileId;
    private string? _statusMessage;
    private string? _errorMessage;
    private bool _includeInactive;
    private bool _isInitialized;

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
    }

    public ObservableCollection<WedgeMatrixRowViewModel> Rows => _rows;

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

    public string Summary { get; } = "Read the wedge reference matrix from included shots and review A1, A2, and A3 setup coverage.";

    public string Footer { get; } = "Phase 1 is read-only. Toggle inactive wedges to compare the full bag without editing references yet.";

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

            if (_golferProfileId == Guid.Empty)
            {
                StatusMessage = "No golfer profile was available.";
                _rows.Clear();
                return;
            }

            var clubs = await _clubRepository.SearchAsync(
                new ClubSearchCriteria(_golferProfileId, ActiveOnly: IncludeInactive ? null : true),
                cancellationToken);
            var references = await _referenceRepository.SearchAsync(_golferProfileId, cancellationToken);

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

            _rows.Clear();
            foreach (var row in matrix.Rows)
            {
                _rows.Add(new WedgeMatrixRowViewModel(row));
            }

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

    private static int CountCells(WedgeMatrixRow row) =>
        new[] { row.A1, row.A2, row.A3 }.Count(cell => cell is not null);

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

    public void Dispose()
    {
        _refreshGate.Dispose();
    }
}
