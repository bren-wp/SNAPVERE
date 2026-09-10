using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snapvere.App.Services;
using Snapvere.Application.Capture;
using Snapvere.Capture.Hotkeys;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;

namespace Snapvere.App;

/// <summary>
/// Hidden lifetime/capture coordinator for the tray-first application.
///
/// This type intentionally is not a user-facing launcher. Normal SNAPVERE
/// startup keeps it hidden while the tray icon and global hotkeys invoke the
/// real Region, Window and Screen capture workflows. Keeping one WinUI Window
/// alive provides a stable application lifetime anchor without reintroducing
/// the legacy Capture Center UX.
/// </summary>
public sealed class CaptureCenterWindow : Window
{
    private readonly ScreenCaptureWorkflow _screenCaptureWorkflow;
    private readonly RegionCaptureWorkflow _regionCaptureWorkflow;
    private readonly WindowCaptureWorkflow _windowCaptureWorkflow;
    private readonly WindowTargetPicker _windowTargetPicker;
    private readonly CapturePreferencesService _capturePreferencesService;
    private readonly CaptureSaveLocationService _captureSaveLocationService;
    private readonly PngCaptureEncoder _pngEncoder;

    private RegionCaptureWindow? _regionCaptureWindow;
    private bool _captureInProgress;

    public CaptureCenterWindow(
        ScreenCaptureWorkflow screenCaptureWorkflow,
        RegionCaptureWorkflow regionCaptureWorkflow,
        WindowCaptureWorkflow windowCaptureWorkflow,
        WindowTargetPicker windowTargetPicker,
        CapturePreferencesService capturePreferencesService,
        CapturePathProvider capturePathProvider,
        PngCaptureEncoder pngEncoder)
    {
        _screenCaptureWorkflow = screenCaptureWorkflow ?? throw new ArgumentNullException(nameof(screenCaptureWorkflow));
        _regionCaptureWorkflow = regionCaptureWorkflow ?? throw new ArgumentNullException(nameof(regionCaptureWorkflow));
        _windowCaptureWorkflow = windowCaptureWorkflow ?? throw new ArgumentNullException(nameof(windowCaptureWorkflow));
        _windowTargetPicker = windowTargetPicker ?? throw new ArgumentNullException(nameof(windowTargetPicker));
        _capturePreferencesService = capturePreferencesService ?? throw new ArgumentNullException(nameof(capturePreferencesService));
        _captureSaveLocationService = new CaptureSaveLocationService(
            capturePathProvider ?? throw new ArgumentNullException(nameof(capturePathProvider)));
        _pngEncoder = pngEncoder ?? throw new ArgumentNullException(nameof(pngEncoder));

        Title = "SNAPVERE Runtime Host";
        Content = BuildRuntimeHostContent();
    }

    public void StartCaptureFromHotkey(CaptureMode mode)
    {
        switch (mode)
        {
            case CaptureMode.Region:
                _ = ExecuteRegionCaptureAsync();
                break;
            case CaptureMode.Window:
                _ = ExecuteWindowCaptureAsync();
                break;
            case CaptureMode.FullScreen:
            case CaptureMode.Monitor:
                _ = ExecuteScreenCaptureAsync();
                break;
        }
    }

    public void ApplyHotkeyRegistrationReport(GlobalHotkeyRegistrationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (!report.HasConflicts)
        {
            StartupDiagnostics.WriteLine("Global capture hotkeys registered successfully.");
            return;
        }

        var conflicts = string.Join(
            ", ",
            report.Conflicts.Select(conflict => conflict.Binding.GestureText));
        StartupDiagnostics.WriteLine($"One or more global capture hotkeys are unavailable: {conflicts}");

        var printScreenConflict = report.Conflicts.Any(
            conflict => string.Equals(conflict.Binding.GestureText, "Print Screen", StringComparison.Ordinal));
        var fallbackRegistered = report.Registered.Any(
            binding => string.Equals(binding.GestureText, "Ctrl+Shift+1", StringComparison.Ordinal));

        if (printScreenConflict && fallbackRegistered)
        {
            StartupDiagnostics.WriteLine("Print Screen is unavailable; Ctrl+Shift+1 remains registered for Region Capture.");
        }
    }

    public void ReportHotkeyHostFailure()
        => StartupDiagnostics.WriteLine(
            "Global hotkey host is unavailable. Tray capture actions remain available.");

    public void ReportTrayHostFailure()
        => StartupDiagnostics.WriteLine(
            "System tray host is unavailable. Registered global hotkeys remain available.");

    private static FrameworkElement BuildRuntimeHostContent()
    {
        // This surface is used only by the explicit startup construction probe.
        // Normal application startup never activates or shows this window.
        var root = new Grid
        {
            RequestedTheme = ElementTheme.Dark,
            Width = 320,
            Height = 160
        };

        root.Children.Add(new TextBlock
        {
            Text = "SNAPVERE runtime host",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.01
        });
        return root;
    }

    private async Task ExecuteRegionCaptureAsync()
    {
        if (!TryBeginCapture())
        {
            return;
        }

        try
        {
            var includeCursor = _capturePreferencesService.Current.IncludeCursorOnCapture;
            var session = await _regionCaptureWorkflow.PreparePrimaryDisplayAsync(includeCursor);
            var overlay = new RegionCaptureWindow(_regionCaptureWorkflow, _pngEncoder, session);
            _regionCaptureWindow = overlay;

            var outcome = await overlay.ShowAsync();
            if (outcome.CopiedToClipboard)
            {
                StartupDiagnostics.WriteLine("Region capture completed and copied to the clipboard.");
            }
            else if (outcome.IsCancelled || outcome.SaveResult is null)
            {
                StartupDiagnostics.WriteLine("Region capture was cancelled.");
            }
            else
            {
                var result = await _captureSaveLocationService.ChooseFinalLocationAsync(this, outcome.SaveResult);
                StartupDiagnostics.WriteLine(
                    $"Region capture saved {result.Width}x{result.Height} PNG to '{result.FilePath}'.");
            }
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Region capture", exception);
        }
        finally
        {
            _regionCaptureWindow = null;
            EndCapture();
        }
    }

    private async Task ExecuteWindowCaptureAsync()
    {
        if (!WindowsGraphicsCaptureService.IsSupported())
        {
            StartupDiagnostics.WriteLine(
                "Window Capture is unavailable because Windows.Graphics.Capture CreateForWindow is not supported on this Windows build.");
            return;
        }

        if (!TryBeginCapture())
        {
            return;
        }

        try
        {
            var target = await _windowTargetPicker.PickAsync();
            if (target is null)
            {
                StartupDiagnostics.WriteLine("Window capture was cancelled.");
                return;
            }

            var includeCursor = _capturePreferencesService.Current.IncludeCursorOnCapture;
            var saved = await _windowCaptureWorkflow.CaptureWindowToDefaultFolderAsync(
                target,
                includeCursor);
            var result = await _captureSaveLocationService.ChooseFinalLocationAsync(this, saved);
            StartupDiagnostics.WriteLine(
                $"Window capture saved {result.Width}x{result.Height} PNG to '{result.FilePath}' from '{target.Title}'.");
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Window capture", exception);
        }
        finally
        {
            EndCapture();
        }
    }

    private async Task ExecuteScreenCaptureAsync()
    {
        if (!TryBeginCapture())
        {
            return;
        }

        try
        {
            var includeCursor = _capturePreferencesService.Current.IncludeCursorOnCapture;
            var saved = await _screenCaptureWorkflow.CapturePrimaryDisplayToDefaultFolderAsync(includeCursor);
            var result = await _captureSaveLocationService.ChooseFinalLocationAsync(this, saved);
            StartupDiagnostics.WriteLine(
                $"Screen capture saved {result.Width}x{result.Height} PNG to '{result.FilePath}'.");
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Screen capture", exception);
        }
        finally
        {
            EndCapture();
        }
    }

    private bool TryBeginCapture()
    {
        if (_captureInProgress)
        {
            StartupDiagnostics.WriteLine("Capture request ignored because another capture is already active.");
            return false;
        }

        _captureInProgress = true;
        return true;
    }

    private void EndCapture()
        => _captureInProgress = false;
}
