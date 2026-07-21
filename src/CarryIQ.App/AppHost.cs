using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CarryIQ.App;

public static class AppHost
{
    public static IHost BuildHost()
    {
        return Host.CreateDefaultBuilder(Array.Empty<string>())
            .ConfigureServices(services => services.AddCarryIqServices())
            .Build();
    }
}
