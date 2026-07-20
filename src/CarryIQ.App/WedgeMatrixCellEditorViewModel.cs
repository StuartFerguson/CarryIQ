using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CarryIQ.App;

public sealed partial class WedgeMatrixCellEditorViewModel : ObservableObject
{
    private Guid? _referenceId;
    private string _targetDistanceText = string.Empty;
    private string _averageCarryText = string.Empty;
    private string _standardDeviationText = string.Empty;
    private string _sampleSizeText = string.Empty;

    public WedgeMatrixCellEditorViewModel(WedgeSetupType setupType)
    {
        SetupType = setupType;
    }

    public WedgeSetupType SetupType { get; }

    public Guid? ReferenceId => _referenceId;

    public string TargetDistanceText
    {
        get => _targetDistanceText;
        set
        {
            if (SetProperty(ref _targetDistanceText, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public string AverageCarryText
    {
        get => _averageCarryText;
        set
        {
            if (SetProperty(ref _averageCarryText, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public string StandardDeviationText
    {
        get => _standardDeviationText;
        set
        {
            if (SetProperty(ref _standardDeviationText, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public string SampleSizeText
    {
        get => _sampleSizeText;
        set
        {
            if (SetProperty(ref _sampleSizeText, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(TargetDistanceText) &&
        string.IsNullOrWhiteSpace(AverageCarryText) &&
        string.IsNullOrWhiteSpace(StandardDeviationText) &&
        string.IsNullOrWhiteSpace(SampleSizeText);

    public void Load(WedgeSwingReference? reference)
    {
        _referenceId = reference?.Id;
        TargetDistanceText = reference?.TargetDistance is Distance targetDistance ? FormatDistance(targetDistance) : string.Empty;
        AverageCarryText = reference?.AverageCarry is Distance averageCarry ? FormatDistance(averageCarry) : string.Empty;
        StandardDeviationText = reference?.CarryStandardDeviation is Distance standardDeviation ? FormatDistance(standardDeviation) : string.Empty;
        SampleSizeText = reference?.SampleSize.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
    }

    public WedgeSwingReference? ToReference(Guid golferProfileId, Guid clubId)
    {
        if (IsEmpty)
        {
            return null;
        }

        return new WedgeSwingReference
        {
            Id = _referenceId ?? Guid.NewGuid(),
            GolferProfileId = golferProfileId,
            ClubId = clubId,
            SwingLabel = SetupType.ToString(),
            SwingType = SwingType.Full,
            ClockPosition = null,
            TargetDistance = ParseDistance(TargetDistanceText),
            AverageCarry = ParseDistance(AverageCarryText),
            CarryStandardDeviation = ParseDistance(StandardDeviationText),
            SampleSize = ParseInt32(SampleSizeText),
            IsManualOverride = true,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static Distance? ParseDistance(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : Distance.FromYards(decimal.Parse(value, NumberStyles.Float, CultureInfo.CurrentCulture));

    private static int ParseInt32(string value) =>
        string.IsNullOrWhiteSpace(value) ? 0 : int.Parse(value, NumberStyles.Integer, CultureInfo.CurrentCulture);

    private static string FormatDistance(Distance distance) =>
        distance.Yards.ToString("0.#", CultureInfo.CurrentCulture);
}
