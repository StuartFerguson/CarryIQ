namespace CarryIQ.Application;

public static class ClubRules
{
    public static IReadOnlyList<string> Validate(Club club)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(club.Name))
        {
            errors.Add("Club name is required.");
        }

        if (club.SortOrder < 0)
        {
            errors.Add("Sort order must be zero or greater.");
        }

        if (club.Loft is not null && club.Loft < 0m)
        {
            errors.Add("Loft cannot be negative.");
        }

        if (club.Length is not null && club.Length.Value.Yards <= 0m)
        {
            errors.Add("Length must be greater than zero when provided.");
        }

        return errors;
    }

    public static bool IsValid(Club club) => Validate(club).Count == 0;
}
