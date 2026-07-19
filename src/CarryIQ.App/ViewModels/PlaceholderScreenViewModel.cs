namespace CarryIQ.App;

public sealed class PlaceholderScreenViewModel(
    string title,
    string summary,
    IReadOnlyList<string> highlights,
    string footer) : IShellScreenViewModel
{
    public string Title { get; } = title;

    public string Summary { get; } = summary;

    public IReadOnlyList<string> Highlights { get; } = highlights;

    public string Footer { get; } = footer;
}
