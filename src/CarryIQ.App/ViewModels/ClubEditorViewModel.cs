using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace CarryIQ.App;

public sealed class ClubEditorViewModel : ObservableObject
{
    private Guid _id;
    private Guid _golferProfileId;
    private string _name = string.Empty;
    private ClubType _clubType;
    private string? _manufacturer;
    private string? _model;
    private decimal? _loft;
    private string? _shaft;
    private string? _shaftFlex;
    private decimal? _lengthYards;
    private bool _isActive;
    private int _sortOrder;
    private string? _notes;
    private DateTimeOffset _createdAt;
    private DateTimeOffset _updatedAt;

    public IReadOnlyList<ClubType> ClubTypes { get; } = Enum.GetValues<ClubType>();

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

    public ClubType ClubType
    {
        get => _clubType;
        set => SetProperty(ref _clubType, value);
    }

    public string? Manufacturer
    {
        get => _manufacturer;
        set => SetProperty(ref _manufacturer, value);
    }

    public string? Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }

    public decimal? Loft
    {
        get => _loft;
        set => SetProperty(ref _loft, value);
    }

    public string? Shaft
    {
        get => _shaft;
        set => SetProperty(ref _shaft, value);
    }

    public string? ShaftFlex
    {
        get => _shaftFlex;
        set => SetProperty(ref _shaftFlex, value);
    }

    public decimal? LengthYards
    {
        get => _lengthYards;
        set => SetProperty(ref _lengthYards, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public int SortOrder
    {
        get => _sortOrder;
        set => SetProperty(ref _sortOrder, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
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

    public bool HasValidationErrors => ClubRules.Validate(ToClub()).Count > 0;

    public string? ValidationMessage
    {
        get
        {
            var errors = ClubRules.Validate(ToClub());
            return errors.Count == 0 ? null : string.Join(Environment.NewLine, errors);
        }
    }

    public void StartNew(Guid golferProfileId, int sortOrder)
    {
        Id = Guid.NewGuid();
        GolferProfileId = golferProfileId;
        Name = string.Empty;
        ClubType = ClubType.Other;
        Manufacturer = null;
        Model = null;
        Loft = null;
        Shaft = null;
        ShaftFlex = null;
        LengthYards = null;
        IsActive = true;
        SortOrder = sortOrder;
        Notes = null;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Load(Club club)
    {
        Id = club.Id;
        GolferProfileId = club.GolferProfileId;
        Name = club.Name;
        ClubType = club.ClubType;
        Manufacturer = club.Manufacturer;
        Model = club.Model;
        Loft = club.Loft;
        Shaft = club.Shaft;
        ShaftFlex = club.ShaftFlex;
        LengthYards = club.Length?.Yards;
        IsActive = club.IsActive;
        SortOrder = club.SortOrder;
        Notes = club.Notes;
        CreatedAt = club.CreatedAt;
        UpdatedAt = club.UpdatedAt;
    }

    public Club ToClub() =>
        new()
        {
            Id = Id,
            GolferProfileId = GolferProfileId,
            Name = Name.Trim(),
            ClubType = ClubType,
            Manufacturer = string.IsNullOrWhiteSpace(Manufacturer) ? null : Manufacturer.Trim(),
            Model = string.IsNullOrWhiteSpace(Model) ? null : Model.Trim(),
            Loft = Loft,
            Shaft = string.IsNullOrWhiteSpace(Shaft) ? null : Shaft.Trim(),
            ShaftFlex = string.IsNullOrWhiteSpace(ShaftFlex) ? null : ShaftFlex.Trim(),
            Length = LengthYards is null ? null : Distance.FromYards(LengthYards.Value),
            IsActive = IsActive,
            SortOrder = SortOrder,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            CreatedAt = CreatedAt,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(HasValidationErrors) or nameof(ValidationMessage))
        {
            return;
        }

        base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasValidationErrors)));
        base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(ValidationMessage)));
    }
}
