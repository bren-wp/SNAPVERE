using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Capture.Windows;
using Snapvere.Imaging;

namespace Snapvere.App;

public partial class App : Application
{
    private readonly ServiceProvider _services;
    private Window? _window;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddSingleton<IDisplayDiscovery, Win32DisplayDiscovery>();
        services.AddSingleton<IScreenCaptureService, GdiScreenCaptureService>();
        services.AddSingleton<PngCaptureEncoder>();
        services.AddSingleton(new CapturePathProvider());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ScreenCaptureWorkflow>();
        services.AddTransient<MainWindow>();

        _services = services.BuildServiceProvider(validateScopes: true);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = _services.GetRequiredService<MainWindow>();
        _window.Closed += OnMainWindowClosed;
        _window.Activate();
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        _window = null;
        _services.Dispose();
    }
}
