using Microsoft.UI.Xaml;

namespace CarryIQ.App.WinUI;

public partial class App : global::Microsoft.Maui.MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
