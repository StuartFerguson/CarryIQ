using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace CarryIQ.App;

public sealed class PracticeSessionEditorViewModel : ObservableObject
{
    private Guid _id;
    private Guid _golferProfileId;
    private string _name = string.Empty;
    private DateOnly _sessionDate;
    private TimeOnly? _startTime;
    private TimeOnly? _endTime;
    private string? _locationName;
    private SessionType _sessionType;
    private SurfaceType _surfaceType;
    private string? _ballType;
    private string? _launchMonitorSource;
    private string? _weatherDescription;
    private decimal? _temperatureCelsius;
    private decimal? _windSpeedMilesPerHour;
    private string? _windDirection;
    private decimal? _elevationMetres;
    private string? _notes;
    private bool _isArchived;
    private DateTimeOffset _createdAt;
    private DateTimeOffset _updatedAt;
    private readonly IReadOnlyList<TimeOnly> _timeOptions = BuildQuarterHourTimeOptions();

    public PracticeSessionEditorViewModel()
    {
        SessionTypes = Enum.GetValues<SessionType>();
        SurfaceTypes = Enum.GetValues<SurfaceType>();
    }

    public IReadOnlyList<SessionType> SessionTypes { get; }

    public IReadOnlyList<SurfaceType> SurfaceTypes { get; }

    public IReadOnlyList<TimeOnly> TimeOptions => _timeOptions;

    public Guid Id
    {
        get => _id;
        private set => SetProperty(ref _id, value);
    }

    public Guid GolferProfileId
    {
        get => _golferProfileId;
        private set => SetProperty(ref _golferProfileId, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public DateOnly SessionDate
    {
        get => _sessionDate;
        set => SetProperty(ref _sessionDate, value);
    }

    public TimeOnly? StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    public TimeOnly? EndTime
    {
        get => _endTime;
        set => SetProperty(ref _endTime, value);
    }

    public string? LocationName
    {
        get => _locationName;
        set => SetProperty(ref _locationName, value);
    }

    public SessionType SessionType
    {
        get => _sessionType;
        set => SetProperty(ref _sessionType, value);
    }

    public SurfaceType SurfaceType
    {
        get => _surfaceType;
        set => SetProperty(ref _surfaceType, value);
    }

    public string? BallType
    {
        get => _ballType;
        set => SetProperty(ref _ballType, value);
    }

    public string? LaunchMonitorSource
    {
        get => _launchMonitorSource;
        set => SetProperty(ref _launchMonitorSource, value);
    }

    public string? WeatherDescription
    {
        get => _weatherDescription;
        set => SetProperty(ref _weatherDescription, value);
    }

    public decimal? TemperatureCelsius
    {
        get => _temperatureCelsius;
        set => SetProperty(ref _temperatureCelsius, value);
    }

    public decimal? WindSpeedMilesPerHour
    {
        get => _windSpeedMilesPerHour;
        set => SetProperty(ref _windSpeedMilesPerHour, value);
    }

    public string? WindDirection
    {
        get => _windDirection;
        set => SetProperty(ref _windDirection, value);
    }

    public decimal? ElevationMetres
    {
        get => _elevationMetres;
        set => SetProperty(ref _elevationMetres, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool IsArchived
    {
        get => _isArchived;
        set => SetProperty(ref _isArchived, value);
    }

    public DateTimeOffset CreatedAt
    {
        get => _createdAt;
        private set => SetProperty(ref _createdAt, value);
    }

    public DateTimeOffset UpdatedAt
    {
        get => _updatedAt;
        private set => SetProperty(ref _updatedAt, value);
    }

    public string? ValidationMessage
    {
        get
        {
            var errors = SessionRules.Validate(ToPracticeSession());
            return errors.Count == 0 ? null : string.Join(Environment.NewLine, errors);
        }
    }

    public void StartNew(Guid golferProfileId)
    {
        var now = DateTimeOffset.UtcNow;

        Id = Guid.NewGuid();
        GolferProfileId = golferProfileId;
        Name = string.Empty;
        SessionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        StartTime = null;
        EndTime = null;
        LocationName = null;
        SessionType = SessionType.DrivingRange;
        SurfaceType = SurfaceType.Unknown;
        BallType = null;
        LaunchMonitorSource = null;
        WeatherDescription = null;
        TemperatureCelsius = null;
        WindSpeedMilesPerHour = null;
        WindDirection = null;
        ElevationMetres = null;
        Notes = null;
        IsArchived = false;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void Load(PracticeSession session)
    {
        Id = session.Id;
        GolferProfileId = session.GolferProfileId;
        Name = session.Name;
        SessionDate = session.SessionDate;
        StartTime = session.StartTime;
        EndTime = session.EndTime;
        LocationName = session.LocationName;
        SessionType = session.SessionType;
        SurfaceType = session.SurfaceType;
        BallType = session.BallType;
        LaunchMonitorSource = session.LaunchMonitorSource;
        WeatherDescription = session.WeatherDescription;
        TemperatureCelsius = session.TemperatureCelsius;
        WindSpeedMilesPerHour = session.WindSpeed?.MilesPerHour;
        WindDirection = session.WindDirection;
        ElevationMetres = session.ElevationMetres;
        Notes = session.Notes;
        IsArchived = session.IsArchived;
        CreatedAt = session.CreatedAt;
        UpdatedAt = session.UpdatedAt;
    }

    public void DuplicateFrom(PracticeSession session)
    {
        Load(session);

        var now = DateTimeOffset.UtcNow;
        Id = Guid.NewGuid();
        Name = string.IsNullOrWhiteSpace(session.Name) ? "Copy" : $"{session.Name} Copy";
        IsArchived = false;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public PracticeSession ToPracticeSession() =>
        new()
        {
            Id = Id,
            GolferProfileId = GolferProfileId,
            Name = Name.Trim(),
            SessionDate = SessionDate,
            StartTime = StartTime,
            EndTime = EndTime,
            LocationName = string.IsNullOrWhiteSpace(LocationName) ? null : LocationName.Trim(),
            SessionType = SessionType,
            SurfaceType = SurfaceType,
            BallType = string.IsNullOrWhiteSpace(BallType) ? null : BallType.Trim(),
            LaunchMonitorSource = string.IsNullOrWhiteSpace(LaunchMonitorSource) ? null : LaunchMonitorSource.Trim(),
            WeatherDescription = string.IsNullOrWhiteSpace(WeatherDescription) ? null : WeatherDescription.Trim(),
            TemperatureCelsius = TemperatureCelsius,
            WindSpeed = WindSpeedMilesPerHour is null ? null : Speed.FromMilesPerHour(WindSpeedMilesPerHour.Value),
            WindDirection = string.IsNullOrWhiteSpace(WindDirection) ? null : WindDirection.Trim(),
            ElevationMetres = ElevationMetres,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            IsArchived = IsArchived,
            CreatedAt = CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(ValidationMessage))
        {
            return;
        }

        base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(ValidationMessage)));
    }

    private static List<TimeOnly> BuildQuarterHourTimeOptions()
    {
        var times = new List<TimeOnly>(96);

        for (var hour = 0; hour < 24; hour++)
        {
            for (var minute = 0; minute < 60; minute += 15)
            {
                times.Add(new TimeOnly(hour, minute));
            }
        }

        return times;
    }
}
