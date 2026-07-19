using System.Windows;
using Microsoft.Extensions.Hosting;

namespace CarryIQ.App;

public partial class App : global::System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = AppHost.BuildHost();
        try
        {
            var initializer = _host.Services.GetRequiredService<IDatabaseInitializer>();
            await initializer.InitializeAsync(CancellationToken.None);
            var mainWindowViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
            await mainWindowViewModel.InitializeAsync(CancellationToken.None);
        }
        catch
        {
            MessageBox.Show(
                "CarryIQ could not initialize its local database. Restart the application and try again.",
                "CarryIQ startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            _host.Dispose();
            _host = null;
            Shutdown(1);
            return;
        }

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
            _host = null;
        }

        base.OnExit(e);
    }
}
