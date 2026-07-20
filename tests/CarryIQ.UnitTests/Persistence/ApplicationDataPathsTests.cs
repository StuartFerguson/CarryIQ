using CarryIQ.Infrastructure;

namespace CarryIQ.UnitTests.Persistence;

public class ApplicationDataPathsTests
{
    [Fact]
    public void CreateBuildsTheCarryIqFolderLayoutFromAnyRoot()
    {
        var layout = ApplicationDataPaths.Create(@"C:\Users\stuar\AppData\Local");

        Assert.Equal(@"C:\Users\stuar\AppData\Local\CarryIQ", layout.DataDirectory);
        Assert.Equal(@"C:\Users\stuar\AppData\Local\CarryIQ\carryiq.duckdb", layout.DatabasePath);
        Assert.Equal(@"C:\Users\stuar\AppData\Local\CarryIQ\user-settings.json", layout.SettingsPath);
        Assert.Equal(@"C:\Users\stuar\AppData\Local\CarryIQ\logs", layout.LogsDirectory);
        Assert.Equal(@"C:\Users\stuar\AppData\Local\CarryIQ\backups", layout.BackupsDirectory);
    }
}
