using System.Globalization;

namespace CarryIQ.App;

public sealed class WedgeMatrixCellViewModel
{
    private WedgeMatrixCellViewModel(
        string setupLabel,
        string targetDistanceText,
        string averageCarryText,
        string standardDeviationText,
        string sampleSizeText,
        bool isManualOverride,
        bool isEmpty)
    {
        SetupLabel = setupLabel;
        TargetDistanceText = targetDistanceText;
        AverageCarryText = averageCarryText;
        StandardDeviationText = standardDeviationText;
        SampleSizeText = sampleSizeText;
        IsManualOverride = isManualOverride;
        IsEmpty = isEmpty;
    }

    public string SetupLabel { get; }

    public string TargetDistanceText { get; }

    public string AverageCarryText { get; }

    public string StandardDeviationText { get; }

    public string SampleSizeText { get; }

    public bool IsManualOverride { get; }

    public bool IsEmpty { get; }

    public static WedgeMatrixCellViewModel Missing(WedgeSetupType setupType)
    {
        var label = setupType.ToString();
        return new WedgeMatrixCellViewModel(label, "N/A", "N/A", "N/A", "N/A", false, true);
    }

    public static WedgeMatrixCellViewModel From(WedgeMatrixCell cell)
    {
        return new WedgeMatrixCellViewModel(
            cell.SetupType.ToString(),
            FormatDistance(cell.TargetDistance),
            FormatDistance(cell.AverageCarry),
            FormatDistance(cell.CarryStandardDeviation),
            $"n={cell.SampleSize.ToString(CultureInfo.InvariantCulture)}",
            cell.IsManualOverride,
            false);
    }

    private static string FormatDistance(Distance? distance) =>
        distance is null ? "N/A" : $"{distance.Value.Yards:0.#} yd";
}
