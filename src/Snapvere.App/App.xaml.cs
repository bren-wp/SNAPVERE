using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Snapvere.App.Services;
using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Capture.Hotkeys;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Snapvere.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private const string StartupProbeEnvironmentVariable = "SNAPVERE_STARTUP_PROBE";
    private const string StartupProbeMarkerFileName = "startup-probe.ready";
    private const string TrayStartupProbeEnvironmentVariable = "SNAPVERE_TRAY_STARTUP_PROBE";
    private const string TrayStartupProbeMarkerFileName = "tray-startup-probe.ready";
    private const string RegionOverlayProbeEnvironmentVariable = "SNAPVERE_REGION_OVERLAY_PROBE";
    private const string RegionOverlayProbeMarkerFileName = "region-overlay-probe.ready";
    private const string WindowOverlayProbeEnvironmentVariable = "SNAPVERE_WINDOW_OVERLAY_PROBE";
    private const string WindowOverlayProbeMarkerFileName = "window-overlay-probe.ready";
    private const string BackgroundStartupArgument = "--background";

    private readonly ServiceProvider _services;
    private CaptureCenterWindow? _window;
    private RegionCaptureWindow? _regionProbeWindow;
    private WindowTargetOverlayWindow? _windowProbeWindow;
    private TrayMenuWindow? _trayMenuWindow;
    private OptionsWindow? _optionsWindow;
    private AboutWindow? _aboutWindow;
    private IGlobalHotkeyService? _hotkeyService;
    private ITrayIconService? _trayIconService;
    private DispatcherQueue? _dispatcherQueue;

    public App()
    {
        StartupDiagnostics.Initialize();

        try
        {
            InitializeComponent();
            UnhandledException += OnUnhandledException;

            var services = new ServiceCollection();
            services.AddSingleton<IDisplayDiscovery, Win32DisplayDiscovery>();
            services.AddSingleton<IWindowDiscovery, Win32WindowDiscovery>();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<GdiScreenCaptureService>();
            services.AddSingleton<WindowsGraphicsCaptureService>();
            services.AddSingleton<IScreenCaptureService>(provider =>
                new ResilientScreenCaptureService(
                    provider.GetRequiredService<WindowsGraphicsCaptureService>(),
                    provider.GetRequiredService<GdiScreenCaptureService>()));
            services.AddSingleton<IWindowCaptureService>(provider =>
                provider.GetRequiredService<WindowsGraphicsCaptureService>());
            services.AddSingleton<PngCaptureEncoder>();
            services.AddSingleton(new CapturePathProvider());
            services.AddSingleton<CaptureFileWriter>();
            services.AddSingleton<CapturePreferencesService>();
            services.AddSingleton<ScreenCaptureWorkflow>();
            services.AddSingleton<RegionCaptureWorkflow>();
            services.AddSingleton<WindowCaptureWorkflow>();
            services.AddSingleton<WindowTargetPicker>();
            services.AddSingleton<CaptureHistoryService>();
            services.AddSingleton<StartupRegistrationService>();
            services.AddSingleton<IGlobalHotkeyService>(
                _ => new Win32GlobalHotkeyService(DefaultCaptureHotkeys.ImplementedNow));
            services.AddSingleton<ITrayIconService, Win32TrayIconService>();
            services.AddTransient<CaptureCenterWindow>();
            services.AddTransient<OptionsWindow>();

            _services = services.BuildServiceProvider(validateScopes: true);
            StartupDiagnostics.WriteLine("Application services initialized. WGC is preferred with GDI fallback for monitor acquisition.");
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
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
                ?? throw new InvalidOperationException("SNAPVERE could not acquire the WinUI dispatcher queue.");

            if (IsRegionOverlayProbeRequested())
            {
                StartRegionOverlayProbe();
                return;
            }

            if (IsWindowOverlayProbeRequested())
            {
                StartWindowOverlayProbe();
                return;
            }

            _window = _services.GetRequiredService<CaptureCenterWindow>();
            _window.Closed += OnMainWindowClosed;
            StartupDiagnostics.WriteLine("Capture coordinator created.");

            if (IsStartupProbeRequested())
            {
                _window.Activate();
                StartupDiagnostics.WriteLine("Main window activated for startup validation probe.");
                CompleteStartupProbe();
                return;
            }

            StartupDiagnostics.WriteLine("Starting global hotkey host.");
            StartGlobalHotkeys();
            StartupDiagnostics.WriteLine("Global hotkey host startup completed.");

            StartupDiagnostics.WriteLine("Starting system tray host.");
            StartTrayIcon();
            StartupDiagnostics.WriteLine("System tray host startup completed.");

            if (IsTrayStartupProbeRequested())
            {
                CompleteTrayStartupProbe();
                return;
            }

            if (IsBackgroundStartupRequested())
            {
                StartupDiagnostics.WriteLine("Background startup requested. Capture Center remains hidden in the tray.");
                return;
            }

            _window.ShowFromTray();
            StartupDiagnostics.WriteLine("Manual launch detected. Capture Center shown to the user.");
        }
        catch (Exception exception)
        {
            StartupDiagnostics.ShowFatal("Application launch", exception);
            throw;
        }
    }

    private static bool IsStartupProbeRequested()
    {
        if (string.Equals(Environment.GetEnvironmentVariable(StartupProbeEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return true;
        }

        return Environment.GetCommandLineArgs().Any(
            argument => string.Equals(argument, "--startup-probe", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTrayStartupProbeRequested()
    {
        if (string.Equals(Environment.GetEnvironmentVariable(TrayStartupProbeEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return true;
        }

        return Environment.GetCommandLineArgs().Any(
            argument => string.Equals(argument, "--tray-startup-probe", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRegionOverlayProbeRequested()
    {
        if (string.Equals(Environment.GetEnvironmentVariable(RegionOverlayProbeEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return true;
        }

        return Environment.GetCommandLineArgs().Any(
            argument => string.Equals(argument, "--region-overlay-probe", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsWindowOverlayProbeRequested()
    {
        if (string.Equals(Environment.GetEnvironmentVariable(WindowOverlayProbeEnvironmentVariable), "1", StringComparison.Ordinal))
        {
            return true;
        }

        return Environment.GetCommandLineArgs().Any(
            argument => string.Equals(argument, "--window-overlay-probe", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBackgroundStartupRequested()
        => Environment.GetCommandLineArgs().Any(
            argument => string.Equals(argument, BackgroundStartupArgument, StringComparison.OrdinalIgnoreCase));

    private static void CompleteStartupProbe()
    {
        WriteProbeMarker(
            StartupProbeMarkerFileName,
            "READY",
            "Startup probe reached activated Capture Center.");
    }

    private static void CompleteTrayStartupProbe()
    {
        WriteProbeMarker(
            TrayStartupProbeMarkerFileName,
            "TRAY_READY",
            "Tray-first startup probe initialized native tray and hotkey hosts without showing Capture Center.");
    }

    private void StartRegionOverlayProbe()
    {
        _window = _services.GetRequiredService<CaptureCenterWindow>();
        _window.Closed += OnMainWindowClosed;
        _window.Activate();
        StartupDiagnostics.WriteLine("Region overlay probe host window activated.");
        _window.AppWindow.Hide();
        StartupDiagnostics.WriteLine("Region overlay probe host window hidden.");

        var probe = CreateProbeDesktop();
        var session = new RegionCaptureSession(probe.Display, probe.Frame);
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

    private void StartWindowOverlayProbe()
    {
        _window = _services.GetRequiredService<CaptureCenterWindow>();
        _window.Closed += OnMainWindowClosed;
        _window.Activate();
        StartupDiagnostics.WriteLine("Window overlay probe host window activated.");
        _window.AppWindow.Hide();
        StartupDiagnostics.WriteLine("Window overlay probe host window hidden.");

        var probe = CreateProbeDesktop();
        var target = new WindowDescriptor(
            new nint(1),
            "SNAPVERE Window Capture CI probe",
            new PixelRect(80, 60, 420, 220),
            4242,
            "SNAPVERE.ProbeWindow");

        _windowProbeWindow = new WindowTargetOverlayWindow(
            probe.Display,
            probe.Frame,
            [target],
            _ => { },
            _ => { },
            () => { });
        _windowProbeWindow.SetTarget(target);

        if (_windowProbeWindow.Content is FrameworkElement root)
        {
            root.Loaded += WindowOverlayProbeRoot_Loaded;
        }

        StartupDiagnostics.WriteLine("Window overlay probe window created.");
        _windowProbeWindow.Show();
        StartupDiagnostics.WriteLine("Window overlay probe activation requested.");
    }

    private static (DisplayDescriptor Display, CaptureFrame Frame) CreateProbeDesktop()
    {
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
        return (display, frame);
    }

    private async void RegionOverlayProbeRoot_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Delay(250);
            WriteProbeMarker(
                RegionOverlayProbeMarkerFileName,
                "REGION_OVERLAY_READY",
                "Region overlay probe loaded editor surface.");
        }
        catch (Exception exception)
        {
            StartupDiagnostics.ShowFatal("Region overlay probe", exception);
            Environment.Exit(1);
        }
    }

    private async void WindowOverlayProbeRoot_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Delay(250);
            WriteProbeMarker(
                WindowOverlayProbeMarkerFileName,
                "WINDOW_OVERLAY_READY",
                "Window overlay probe loaded target-selection surface.");
        }
        catch (Exception exception)
        {
            StartupDiagnostics.ShowFatal("Window overlay probe", exception);
            Environment.Exit(1);
        }
    }

    private static void WriteProbeMarker(string fileName, string state, string logMessage)
    {
        var probeDirectory = Path.Combine(Path.GetTempPath(), "SNAPVERE");
        Directory.CreateDirectory(probeDirectory);
        var markerPath = Path.Combine(probeDirectory, fileName);
        var version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        File.WriteAllText(
            markerPath,
            $"SNAPVERE {version} {state} | PID={Environment.ProcessId} | ARCH={RuntimeInformation.ProcessArchitecture} | {DateTimeOffset.UtcNow:O}");

        StartupDiagnostics.WriteLine($"{logMessage} Marker={markerPath}");
        Environment.Exit(0);
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
        var queue = _dispatcherQueue;
        var window = _window;
        if (queue is null || window is null)
        {
            return;
        }

        _ = queue.TryEnqueue(() => window.StartCaptureFromHotkey(e.Binding.Mode));
    }

    private void OnTrayCommandInvoked(object? sender, TrayCommandEventArgs e)
    {
        var queue = _dispatcherQueue;
        if (queue is null)
        {
            return;
        }

        _ = queue.TryEnqueue(() => ExecuteTrayCommand(e.Command));
    }

    private void ExecuteTrayCommand(TrayCommand command)
    {
        var window = _window;
        if (window is null)
        {
            return;
        }

        switch (command)
        {
            case TrayCommand.ShowMenu:
                ShowTrayMenu();
                break;
            case TrayCommand.Show:
                CloseTrayMenu();
                ShowOptions(OptionsSection.Preferences);
                break;
            case TrayCommand.RegionCapture:
                CloseTrayMenu();
                window.StartCaptureFromHotkey(CaptureMode.Region);
                break;
            case TrayCommand.WindowCapture:
                CloseTrayMenu();
                window.StartCaptureFromHotkey(CaptureMode.Window);
                break;
            case TrayCommand.ScreenCapture:
                CloseTrayMenu();
                window.StartCaptureFromHotkey(CaptureMode.FullScreen);
                break;
            case TrayCommand.OpenCaptureFolder:
                CloseTrayMenu();
                OpenCaptureFolder();
                break;
            case TrayCommand.About:
                CloseTrayMenu();
                ShowAbout();
                break;
            case TrayCommand.Exit:
                CloseTrayMenu();
                _optionsWindow?.Close();
                _aboutWindow?.Close();
                window.Close();
                break;
        }
    }

    private void ShowTrayMenu()
    {
        CloseTrayMenu();
        var menu = new TrayMenuWindow(
            ExecuteTrayCommand,
            () => ShowOptions(OptionsSection.RecentCaptures));
        _trayMenuWindow = menu;
        menu.Closed += (_, _) =>
        {
            if (ReferenceEquals(_trayMenuWindow, menu))
            {
                _trayMenuWindow = null;
            }
        };
        menu.ShowNearTray();
    }

    private void CloseTrayMenu()
    {
        var menu = _trayMenuWindow;
        _trayMenuWindow = null;
        if (menu is not null)
        {
            menu.Close();
        }
    }

    private void ShowOptions(OptionsSection section)
    {
        if (_optionsWindow is not null)
        {
            _optionsWindow.ShowSection(section);
            _optionsWindow.Activate();
            return;
        }

        var options = _services.GetRequiredService<OptionsWindow>();
        _optionsWindow = options;
        options.ShowSection(section);
        options.Closed += (_, _) =>
        {
            if (ReferenceEquals(_optionsWindow, options))
            {
                _optionsWindow = null;
            }
        };
        options.Activate();
    }

    private void ShowAbout()
    {
        if (_aboutWindow is not null)
        {
            _aboutWindow.Activate();
            return;
        }

        var about = new AboutWindow();
        _aboutWindow = about;
        about.Closed += (_, _) =>
        {
            if (ReferenceEquals(_aboutWindow, about))
            {
                _aboutWindow = null;
            }
        };
        about.Activate();
    }

    private void OpenCaptureFolder()
    {
        try
        {
            var history = _services.GetRequiredService<CaptureHistoryService>();
            var directory = history.GetCaptureDirectory();
            Directory.CreateDirectory(directory);
            _ = Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            StartupDiagnostics.Record("Open capture folder from tray", exception);
        }
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        CloseTrayMenu();
        _optionsWindow?.Close();
        _optionsWindow = null;
        _aboutWindow?.Close();
        _aboutWindow = null;

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
        _dispatcherQueue = null;
        UnhandledException -= OnUnhandledException;
        _services.Dispose();
        StartupDiagnostics.WriteLine("Application shutdown completed.");
    }
}
