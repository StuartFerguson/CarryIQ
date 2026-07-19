namespace CarryIQ.UnitTests.Application;

public class ClubRulesTests
{
    [Fact]
    public void ValidateAllowsLoftToBeOptional()
    {
        var club = CreateClub(loft: null);

        var errors = ClubRules.Validate(club);

        Assert.Empty(errors);
        Assert.True(ClubRules.IsValid(club));
    }

    [Fact]
    public void ValidateRejectsBlankName()
    {
        var club = CreateClub(name: "   ");

        var errors = ClubRules.Validate(club);

        Assert.Contains(errors, error => error == "Club name is required.");
        Assert.False(ClubRules.IsValid(club));
    }

    [Fact]
    public void ValidateRejectsNegativeSortOrder()
    {
        var club = CreateClub(sortOrder: -1);

        var errors = ClubRules.Validate(club);

        Assert.Contains(errors, error => error == "Sort order must be zero or greater.");
    }

    private static Club CreateClub(
        string name = "7 Iron",
        decimal? loft = 32m,
        int sortOrder = 4) =>
        new()
        {
            Id = Guid.NewGuid(),
            GolferProfileId = Guid.NewGuid(),
            Name = name,
            ClubType = ClubType.Iron,
            Loft = loft,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
}
