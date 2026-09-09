using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Snapvere.App.Services;
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
    private ITrayIconService? _trayIconService;

    public App()
    {
        StartupDiagnostics.Initialize();

        try
        {
            InitializeComponent();
            UnhandledException += OnUnhandledException;

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
            services.AddSingleton<ITrayIconService, Win32TrayIconService>();
            services.AddTransient<MainWindow>();

            _services = services.BuildServiceProvider(validateScopes: true);
            StartupDiagnostics.WriteLine("Application services initialized.");
        }
        catch (Exception exception)
        {
            StartupDiagnostics.ShowFatal("Application initialization", exception);
            throw;
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = _services.GetRequiredService<MainWindow>();
            _window.Closed += OnMainWindowClosed;
            _window.Activate();
            StartupDiagnostics.WriteLine("Main window activated.");

            if (IsStartupProbeRequested())
            {
                StartupDiagnostics.WriteLine("Startup probe succeeded; terminating probe process cleanly.");
                _ = _window.DispatcherQueue.TryEnqueue(() =>
                {
                    _window?.Close();
                    Environment.ExitCode = 0;
                    Environment.Exit(0);
                });
                return;
            }

            StartGlobalHotkeys();
            StartTrayIcon();
        }
        catch (Exception exception)
        {
            StartupDiagnostics.ShowFatal("Application launch", exception);
            throw;
        }
    }

    private static bool IsStartupProbeRequested()
        => Environment.GetCommandLineArgs().Any(
            argument => string.Equals(argument, "--startup-probe", StringComparison.OrdinalIgnoreCase));

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (e.Exception is not null)
        {
            StartupDiagnostics.Record("XAML.UnhandledException", e.Exception);
        }
    }

    private void StartGlobalHotkeys()
    {
        var window = _window;
        if (window is null)
        {
            return;
        }

        _hotkeyService = _services.GetRequiredService<IGlobalHotkeyService>();
        _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;

        try
        {
            var report = _hotkeyService.Start();
            window.ApplyHotkeyRegistrationReport(report);
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Global hotkey host", exception);
            window.ReportHotkeyHostFailure();
        }
    }

    private void StartTrayIcon()
    {
        var window = _window;
        if (window is null)
        {
            return;
        }

        _trayIconService = _services.GetRequiredService<ITrayIconService>();
        _trayIconService.CommandInvoked += OnTrayCommandInvoked;

        try
        {
            _trayIconService.Start();
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("System tray host", exception);
            window.ReportTrayHostFailure();
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

    private void OnTrayCommandInvoked(object? sender, TrayCommandEventArgs e)
    {
        var window = _window;
        if (window is null)
        {
            return;
        }

        _ = window.DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.Command)
            {
                case TrayCommand.Show:
                    window.ShowFromTray();
                    break;
                case TrayCommand.RegionCapture:
                    window.StartCaptureFromHotkey(Snapvere.Domain.Capture.CaptureMode.Region);
                    break;
                case TrayCommand.ScreenCapture:
                    window.StartCaptureFromHotkey(Snapvere.Domain.Capture.CaptureMode.FullScreen);
                    break;
                case TrayCommand.Exit:
                    window.Close();
                    break;
            }
        });
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        if (_hotkeyService is not null)
        {
            _hotkeyService.HotkeyPressed -= OnGlobalHotkeyPressed;
        }

        if (_trayIconService is not null)
        {
            _trayIconService.CommandInvoked -= OnTrayCommandInvoked;
        }

        _hotkeyService = null;
        _trayIconService = null;
        _window = null;
        UnhandledException -= OnUnhandledException;
        _services.Dispose();
        StartupDiagnostics.WriteLine("Application shutdown completed.");
    }
}
