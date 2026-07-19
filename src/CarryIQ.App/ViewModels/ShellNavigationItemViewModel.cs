namespace CarryIQ.App;

public sealed class ShellNavigationItemViewModel(
    string title,
    string eyebrow,
    string summary,
    PlaceholderScreenViewModel screen)
{
    public string Title { get; } = title;

    public string Eyebrow { get; } = eyebrow;

    public string Summary { get; } = summary;

    public PlaceholderScreenViewModel Screen { get; } = screen;
}
