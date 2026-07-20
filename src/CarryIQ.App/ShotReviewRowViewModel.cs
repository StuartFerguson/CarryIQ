using CommunityToolkit.Mvvm.ComponentModel;

namespace CarryIQ.App;

public sealed partial class ShotReviewRowViewModel : ObservableObject
{
    private bool _isSelected;

    public ShotReviewRowViewModel(Shot shot, string clubName)
    {
        Id = shot.Id;
        PracticeSessionId = shot.PracticeSessionId;
        ShotSequence = shot.ShotSequence;
        RecordedAt = shot.RecordedAt;
        ClubId = shot.ClubId;
        ClubName = clubName;
        IsIncluded = shot.IsIncluded;
        ExclusionReason = shot.ExclusionReason;
        SwingType = shot.SwingType;
        Notes = shot.Notes;
        RawImportData = shot.RawImportData;
        DisplayCarryDistance = shot.CarryDistance?.Yards;
    }

    public Guid Id { get; }

    public Guid PracticeSessionId { get; }

    public int ShotSequence { get; }

    public DateTimeOffset RecordedAt { get; }

    public Guid ClubId { get; private set; }

    public string ClubName { get; private set; }

    public bool IsIncluded { get; private set; }

    public string? ExclusionReason { get; private set; }

    public SwingType? SwingType { get; private set; }

    public string? Notes { get; private set; }

    public string? RawImportData { get; private set; }

    public decimal? DisplayCarryDistance { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string InclusionState => IsIncluded ? "Included" : "Excluded";

    public void ApplyShot(Shot shot, string clubName)
    {
        ClubId = shot.ClubId;
        ClubName = clubName;
        IsIncluded = shot.IsIncluded;
        ExclusionReason = shot.ExclusionReason;
        SwingType = shot.SwingType;
        Notes = shot.Notes;
        RawImportData = shot.RawImportData;
        OnPropertyChanged(nameof(ClubId));
        OnPropertyChanged(nameof(ClubName));
        OnPropertyChanged(nameof(IsIncluded));
        OnPropertyChanged(nameof(ExclusionReason));
        OnPropertyChanged(nameof(SwingType));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(RawImportData));
        OnPropertyChanged(nameof(InclusionState));
    }
}
