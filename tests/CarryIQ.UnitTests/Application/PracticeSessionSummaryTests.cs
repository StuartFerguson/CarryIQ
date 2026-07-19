namespace CarryIQ.UnitTests.Application;

public class PracticeSessionSummaryTests
{
    [Fact]
    public void DisplayDurationFormatsAsHoursAndMinutes()
    {
        var summary = new PracticeSessionSummary(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Morning Range",
            new DateOnly(2026, 7, 19),
            new TimeOnly(9, 15),
            new TimeOnly(10, 30),
            TimeSpan.FromMinutes(75),
            SessionType.DrivingRange,
            "South Range",
            "Trackman",
            12,
            9,
            false);

        Assert.Equal("01:15", summary.DisplayDuration);
    }

    [Fact]
    public void NullableSummaryFieldsUseAStableDashPlaceholder()
    {
        var summary = new PracticeSessionSummary(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Morning Range",
            new DateOnly(2026, 7, 19),
            null,
            null,
            null,
            SessionType.DrivingRange,
            null,
            null,
            0,
            0,
            false);

        Assert.Equal("-", summary.DisplayLocationName);
        Assert.Equal("-", summary.DisplayLaunchMonitorSource);
    }
}
