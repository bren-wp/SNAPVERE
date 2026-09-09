using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Snapvere.App.Services;
using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Capture.Hotkeys;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;
using System.Runtime.InteropServices;

namespace Snapvere.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private const string StartupProbeEnvironmentVariable = "SNAPVERE_STARTUP_PROBE";
    private const string StartupProbeMarkerFileName = "startup-probe.ready";
    private const string RegionOverlayProbeEnvironmentVariable = "SNAPVERE_REGION_OVERLAY_PROBE";
    private const string RegionOverlayProbeMarkerFileName = "region-overlay-probe.ready";

    private readonly ServiceProvider _services;
    private CaptureCenterWindow? _window;
    private RegionCaptureWindow? _regionProbeWindow;
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
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<GdiScreenCaptureService>();
            services.AddSingleton<WindowsGraphicsCaptureService>();
            services.AddSingleton<IScreenCaptureService>(provider =>
                new ResilientScreenCaptureService(
                    provider.GetRequiredService<WindowsGraphicsCaptureService>(),
                    provider.GetRequiredService<GdiScreenCaptureService>()));
            services.AddSingleton<PngCaptureEncoder>();
            services.AddSingleton(new CapturePathProvider());
            services.AddSingleton<CaptureFileWriter>();
            services.AddSingleton<ScreenCaptureWorkflow>();
            services.AddSingleton<RegionCaptureWorkflow>();
            services.AddSingleton<CaptureHistoryService>();
            services.AddSingleton<IGlobalHotkeyService>(
                _ => new Win32GlobalHotkeyService(DefaultCaptureHotkeys.ImplementedNow));
            services.AddSingleton<ITrayIconService, Win32TrayIconService>();
            services.AddTransient<CaptureCenterWindow>();

            _services = services.BuildServiceProvider(validateScopes: true);
            StartupDiagnostics.WriteLine("Application services initialized. WGC is preferred with GDI fallback.");
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
            if (IsRegionOverlayProbeRequested())
            {
                StartRegionOverlayProbe();
                return;
            }

            _window = _services.GetRequiredService<CaptureCenterWindow>();
            _window.Closed += OnMainWindowClosed;

            StartupDiagnostics.WriteLine("Stable Capture Center window created.");
            _window.Activate();
            StartupDiagnostics.WriteLine("Main window activated.");

            if (IsStartupProbeRequested())
            {
                CompleteStartupProbe();
                return;
            }

            StartupDiagnostics.WriteLine("Starting global hotkey host.");
            StartGlobalHotkeys();
            StartupDiagnostics.WriteLine("Global hotkey host startup completed.");

            StartupDiagnostics.WriteLine("Starting system tray host.");
            StartTrayIcon();
            StartupDiagnostics.WriteLine("System tray host startup completed.");
        }
        catch (Exception exception)
        {
            StartupDiagnostics.ShowFatal("Application launch", exception);
            throw;
        }
    }

    private static bool IsStartupProbeRequested()
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable(StartupProbeEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            return true;
        }

        return Environment.GetCommandLineArgs().Any(
            argument => string.Equals(argument, "--startup-probe", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRegionOverlayProbeRequested()
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable(RegionOverlayProbeEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            return true;
        }

        return Environment.GetCommandLineArgs().Any(
            argument => string.Equals(argument, "--region-overlay-probe", StringComparison.OrdinalIgnoreCase));
    }

    private static void CompleteStartupProbe()
    {
        var probeDirectory = Path.Combine(Path.GetTempPath(), "SNAPVERE");
        Directory.CreateDirectory(probeDirectory);

        var markerPath = Path.Combine(probeDirectory, StartupProbeMarkerFileName);
        var version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        File.WriteAllText(
            markerPath,
            $"SNAPVERE {version} READY | PID={Environment.ProcessId} | ARCH={RuntimeInformation.ProcessArchitecture} | {DateTimeOffset.UtcNow:O}");

        StartupDiagnostics.WriteLine($"Startup probe reached activated main window. Marker={markerPath}");
        Environment.Exit(0);
    }

    private void StartRegionOverlayProbe()
    {
        _window = _services.GetRequiredService<CaptureCenterWindow>();
        _window.Closed += OnMainWindowClosed;
        _window.Activate();
        StartupDiagnostics.WriteLine("Region overlay probe host window activated.");
        _window.AppWindow.Hide();
        StartupDiagnostics.WriteLine("Region overlay probe host window hidden.");

        const int width = 640;
        const int height = 360;
        var stride = checked(width * 4);
        var pixels = new byte[checked(stride * height)];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = checked(y * stride + x * 4);
                pixels[offset] = checked((byte)(24 + (x * 36 / width)));
                pixels[offset + 1] = checked((byte)(22 + (y * 34 / height)));
                pixels[offset + 2] = checked((byte)(28 + ((x + y) * 24 / (width + height))));
                pixels[offset + 3] = byte.MaxValue;
            }
        }

        var bounds = new PixelRect(0, 0, width, height);
        var display = new DisplayDescriptor(
            "probe-display",
            bounds,
            bounds,
            96,
            96,
            true,
            "SNAPVERE CI probe");
        var frame = new CaptureFrame(
            new PixelSize(width, height),
            stride,
            pixels,
            DateTimeOffset.UtcNow,
            "probe");
        var session = new RegionCaptureSession(display, frame);

        var workflow = _services.GetRequiredService<RegionCaptureWorkflow>();
        var encoder = _services.GetRequiredService<PngCaptureEncoder>();
        _regionProbeWindow = new RegionCaptureWindow(workflow, encoder, session);

        if (_regionProbeWindow.Content is FrameworkElement root)
        {
            root.Loaded += RegionOverlayProbeRoot_Loaded;
        }

        StartupDiagnostics.WriteLine("Region overlay probe window created.");
        _ = _regionProbeWindow.ShowAsync();
        StartupDiagnostics.WriteLine("Region overlay probe activation requested.");
    }

    private async void RegionOverlayProbeRoot_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Delay(250);

            var probeDirectory = Path.Combine(Path.GetTempPath(), "SNAPVERE");
            Directory.CreateDirectory(probeDirectory);
            var markerPath = Path.Combine(probeDirectory, RegionOverlayProbeMarkerFileName);
            var version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "unknown";
            File.WriteAllText(
                markerPath,
                $"SNAPVERE {version} REGION_OVERLAY_READY | PID={Environment.ProcessId} | ARCH={RuntimeInformation.ProcessArchitecture} | {DateTimeOffset.UtcNow:O}");

            StartupDiagnostics.WriteLine($"Region overlay probe loaded editor surface. Marker={markerPath}");
            Environment.Exit(0);
        }
        catch (Exception exception)
        {
            StartupDiagnostics.ShowFatal("Region overlay probe", exception);
            Environment.Exit(1);
        }
    }

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
