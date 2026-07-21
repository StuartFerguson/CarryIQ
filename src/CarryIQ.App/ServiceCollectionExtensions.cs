using Microsoft.Extensions.DependencyInjection;

namespace CarryIQ.App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCarryIqServices(this IServiceCollection services)
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
        services.AddSingleton<IDemoDataSeeder, DemoDataSeeder>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ClubManagerViewModel>();
        services.AddSingleton<SessionManagerViewModel>();
        services.AddSingleton<ShotEntryViewModel>();
        services.AddSingleton<ShotReviewViewModel>();
        services.AddSingleton<UtilitiesViewModel>();
        services.AddSingleton<WedgeMatrixViewModel>();
        services.AddSingleton<AnalyticsViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        return services;
    }
}
