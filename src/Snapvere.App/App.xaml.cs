using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Capture.Hotkeys;
using Snapvere.Capture.Windows;
using Snapvere.Imaging;

namespace Snapvere.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private readonly ServiceProvider _services;
    private MainWindow? _window;
    private IGlobalHotkeyService? _hotkeyService;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddSingleton<IDisplayDiscovery, Win32DisplayDiscovery>();
        services.AddSingleton<IScreenCaptureService, GdiScreenCaptureService>();
        services.AddSingleton<PngCaptureEncoder>();
        services.AddSingleton(new CapturePathProvider());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<CaptureFileWriter>();
        services.AddSingleton<ScreenCaptureWorkflow>();
        services.AddSingleton<RegionCaptureWorkflow>();
        services.AddSingleton<CaptureHistoryService>();
        services.AddSingleton<IGlobalHotkeyService>(
            _ => new Win32GlobalHotkeyService(DefaultCaptureHotkeys.ImplementedNow));
        services.AddTransient<MainWindow>();

        _services = services.BuildServiceProvider(validateScopes: true);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = _services.GetRequiredService<MainWindow>();
        _window.Closed += OnMainWindowClosed;
        _window.Activate();

        _hotkeyService = _services.GetRequiredService<IGlobalHotkeyService>();
        _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;

        try
        {
            var report = _hotkeyService.Start();
            _window.ApplyHotkeyRegistrationReport(report);
        }
        catch
        {
            _window.ReportHotkeyHostFailure();
        }
    }

    private void OnGlobalHotkeyPressed(object? sender, CaptureHotkeyPressedEventArgs e)
    {
        var window = _window;
        if (window is null)
        {
            return;
        }

        _ = window.DispatcherQueue.TryEnqueue(
            () => window.StartCaptureFromHotkey(e.Binding.Mode));
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        if (_hotkeyService is not null)
        {
            _hotkeyService.HotkeyPressed -= OnGlobalHotkeyPressed;
        }

        _hotkeyService = null;
        _window = null;
        _services.Dispose();
    }
}
