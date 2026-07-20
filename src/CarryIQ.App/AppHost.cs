using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CarryIQ.App;

public static class AppHost
{
    public static IHost BuildHost()
    {
        return Host.CreateDefaultBuilder(Array.Empty<string>())
            .ConfigureServices(services =>
            {
                services.AddSingleton<IApplicationPaths, ApplicationPaths>();
                services.AddSingleton<IDatabaseConnectionFactory, DuckDbConnectionFactory>();
                services.AddSingleton<DuckDbMigrationRunner>();
                services.AddSingleton<IDatabaseInitializer, DuckDbDatabaseInitializer>();
                services.AddSingleton<IClubRepository, DuckDbClubRepository>();
                services.AddSingleton<IPracticeSessionRepository, DuckDbPracticeSessionRepository>();
                services.AddSingleton<IShotRepository, DuckDbShotRepository>();
                services.AddSingleton<IDashboardProjectionRepository, DuckDbDashboardProjectionRepository>();
                services.AddSingleton<IWedgeSwingReferenceRepository, DuckDbWedgeSwingReferenceRepository>();
                services.AddSingleton<IShotEntryPreferencesStore, JsonShotEntryPreferencesStore>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<ClubManagerViewModel>();
                services.AddSingleton<SessionManagerViewModel>();
                services.AddSingleton<ShotEntryViewModel>();
                services.AddSingleton<ShotReviewViewModel>();
                services.AddSingleton<WedgeMatrixViewModel>();
                services.AddSingleton<AnalyticsViewModel>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }
}
