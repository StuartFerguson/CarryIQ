using CarryIQ.App;
using Microsoft.Extensions.DependencyInjection;

namespace CarryIQ.UnitTests.App;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCarryIqServicesRegistersTheCoreAppGraph()
    {
        var services = new ServiceCollection();

        services.AddCarryIqServices();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IApplicationPaths>());
        Assert.NotNull(provider.GetRequiredService<IDatabaseInitializer>());
        Assert.NotNull(provider.GetRequiredService<IClubRepository>());
        Assert.NotNull(provider.GetRequiredService<MainWindowViewModel>());
    }
}
