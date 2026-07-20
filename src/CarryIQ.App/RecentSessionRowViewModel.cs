namespace CarryIQ.App;

public sealed record RecentSessionRowViewModel(
    Guid SessionId,
    DateOnly SessionDate,
    string Name,
    int TotalShots,
    int IncludedShotCount,
    decimal? AverageCarryYards)
{
    public string SessionDateText => SessionDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    public string AverageCarryText => AverageCarryYards is decimal carry ? $"{carry:0.#} yd" : "N/A";

    public string ShotCountText => $"{IncludedShotCount}/{TotalShots}";
}
