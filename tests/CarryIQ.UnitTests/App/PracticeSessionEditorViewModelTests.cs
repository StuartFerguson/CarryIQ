using CarryIQ.App;

namespace CarryIQ.UnitTests.App;

public class PracticeSessionEditorViewModelTests
{
    [Fact]
    public void TimeOptionsExposeQuarterHourSlotsForTheFullDay()
    {
        var viewModel = new PracticeSessionEditorViewModel();

        Assert.Equal(96, viewModel.TimeOptions.Count);
        Assert.Equal(new TimeOnly(0, 0), viewModel.TimeOptions[0]);
        Assert.Equal(new TimeOnly(23, 45), viewModel.TimeOptions[^1]);
    }
}
