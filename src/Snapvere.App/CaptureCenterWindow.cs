using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Snapvere.App.Services;
using Snapvere.Application.Capture;
using Snapvere.Capture.Hotkeys;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;
using Snapvere.Shared;

namespace Snapvere.App;

/// <summary>
/// Hidden lifetime/capture coordinator for the tray-first application.
///
/// Normal SNAPVERE startup keeps this window hidden and intentionally avoids
/// constructing the capture-service graph or an unused visual tree until the
/// user actually starts a capture. The explicit startup QA probe can still
/// request the tiny runtime-host surface.
/// </summary>
public sealed class CaptureCenterWindow : Window
{
    private const string StartupProbeEnvironmentVariable = "SNAPVERE_STARTUP_PROBE";

    private readonly IServiceProvider _services;
    private readonly CapturePreferencesService _capturePreferencesService;

    private RegionCaptureWindow? _regionCaptureWindow;
    private CaptureFeedbackWindow? _feedbackWindow;
    private readonly ScreenRecordingSessionGate _recordingSessionGate = new();
    private readonly CaptureActivityGate _activityGate = new();

    public event Action<bool>? ScreenRecordingStateChanged;

    public bool IsScreenRecordingActive => _recordingSessionGate.IsActive;

    public CaptureCenterWindow(
        IServiceProvider services,
        CapturePreferencesService capturePreferencesService)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _capturePreferencesService = capturePreferencesService ?? throw new ArgumentNullException(nameof(capturePreferencesService));

        Title = "SNAPVERE Runtime Host";
        Closed += (_, _) =>
        {
            CloseCaptureFeedback();
            _recordingSessionGate.Dispose();
        };
        if (IsStartupProbeRequested())
        {
            Content = BuildRuntimeHostContent();
        }
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

    public void StartScreenRecording()
    {
        if (_recordingSessionGate.IsActive)
        {
            StartupDiagnostics.WriteLine(
                "Screen recording start request ignored because recording is already active.");
            return;
        }

        if (!WindowsGraphicsCaptureService.IsSupported())
        {
            StartupDiagnostics.WriteLine(
                "Screen recording is unavailable because Windows.Graphics.Capture is not supported.");
            ShowCaptureFeedback(CaptureFeedbackKind.RecordingUnsupported);
            return;
        }

        if (!TryBeginCapture())
        {
            return;
        }

        CloseCaptureFeedback();
        var session = _recordingSessionGate.TryBegin();
        if (session is null)
        {
            StartupDiagnostics.WriteLine(
                "Screen recording start request lost ownership to another active recording.");
            EndCapture();
            return;
        }

        NotifyScreenRecordingStateChanged(active: true);
        _ = ExecuteScreenRecordingAsync(session);
    }

    public void StopScreenRecording()
    {
        if (_recordingSessionGate.RequestStop())
        {
            StartupDiagnostics.WriteLine("Screen recording stop requested by the user.");
            return;
        }

        StartupDiagnostics.WriteLine(
            "Screen recording stop request ignored because no stoppable recording is active.");
    }

    private async Task ExecuteScreenRecordingAsync(ScreenRecordingSession session)
    {
        try
        {
            var workflow = _services.GetRequiredService<ScreenRecordingWorkflow>();
            var includeCursor = _capturePreferencesService.Current.IncludeCursorOnCapture;
            var result = await workflow
                .RecordPrimaryDisplayAsync(includeCursor, session.StopToken)
                .ConfigureAwait(true);

            StartupDiagnostics.WriteLine(
                $"Screen recording saved {result.EncodedSize.Width}x{result.EncodedSize.Height} MP4 locally; duration {result.Duration.TotalSeconds:F1}s.");
            ShowCaptureFeedback(CaptureFeedbackKind.RecordingSaved);
        }
        catch (OperationCanceledException) when (session.IsStopRequested)
        {
            StartupDiagnostics.WriteLine(
                "Screen recording stopped before a complete MP4 could be produced.");
        }
        catch (PlatformNotSupportedException exception)
        {
            StartupDiagnostics.Record("Screen recording support", exception);
            ShowCaptureFeedback(CaptureFeedbackKind.RecordingUnsupported);
        }
        catch (CapturePersistenceException exception)
        {
            StartupDiagnostics.Record("Persist screen recording", exception);
            ShowCaptureFeedback(ToPersistenceFeedbackKind(exception.Kind));
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Screen recording", exception);
            ShowCaptureFeedback(CaptureFeedbackKind.RecordingFailed);
        }
        finally
        {
            if (_recordingSessionGate.Complete(session))
            {
                NotifyScreenRecordingStateChanged(active: false);
            }

            EndCapture();
        }
    }

    private void NotifyScreenRecordingStateChanged(bool active)
    {
        try
        {
            ScreenRecordingStateChanged?.Invoke(active);
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Screen recording state notification", exception);
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
            var workflow = _services.GetRequiredService<RegionCaptureWorkflow>();
            var pngEncoder = _services.GetRequiredService<PngCaptureEncoder>();
            var includeCursor = _capturePreferencesService.Current.IncludeCursorOnCapture;
            var session = await workflow.PrepareInteractiveDisplayAsync(includeCursor);
            var overlay = new RegionCaptureWindow(workflow, pngEncoder, session);
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
                StartupDiagnostics.WriteLine(
                    $"Region capture saved {outcome.SaveResult.Width}x{outcome.SaveResult.Height} PNG locally.");
            }
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Region capture", exception);
            ShowCaptureFeedback(CaptureFeedbackKind.RegionFailed);
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
            ShowCaptureFeedback(CaptureFeedbackKind.WindowUnsupported);
            return;
        }

        if (!TryBeginCapture())
        {
            return;
        }

        try
        {
            var windowTargetPicker = _services.GetRequiredService<WindowTargetPicker>();
            var windowCaptureWorkflow = _services.GetRequiredService<WindowCaptureWorkflow>();
            var target = await windowTargetPicker.PickAsync();
            if (target is null)
            {
                StartupDiagnostics.WriteLine("Window capture was cancelled.");
                return;
            }

            var includeCursor = _capturePreferencesService.Current.IncludeCursorOnCapture;
            var result = await windowCaptureWorkflow.CaptureWindowToDefaultFolderAsync(
                target,
                includeCursor);
            StartupDiagnostics.WriteLine(
                $"Window capture saved {result.Width}x{result.Height} PNG locally from '{target.Title}'.");
        }
        catch (CapturePersistenceException exception)
        {
            StartupDiagnostics.Record("Persist window capture", exception);
            ShowCaptureFeedback(ToPersistenceFeedbackKind(exception.Kind));
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Window capture", exception);
            ShowCaptureFeedback(CaptureFeedbackKind.WindowFailed);
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
            var screenCaptureWorkflow = _services.GetRequiredService<ScreenCaptureWorkflow>();
            var includeCursor = _capturePreferencesService.Current.IncludeCursorOnCapture;
            var result = await screenCaptureWorkflow.CaptureInteractiveDisplayToDefaultFolderAsync(includeCursor);
            StartupDiagnostics.WriteLine(
                $"Screen capture saved {result.Width}x{result.Height} PNG locally.");
        }
        catch (CapturePersistenceException exception)
        {
            StartupDiagnostics.Record("Persist screen capture", exception);
            ShowCaptureFeedback(ToPersistenceFeedbackKind(exception.Kind));
        }
        catch (Exception exception)
        {
            StartupDiagnostics.Record("Screen capture", exception);
            ShowCaptureFeedback(CaptureFeedbackKind.ScreenFailed);
        }
        finally
        {
            EndCapture();
        }
    }

    private static CaptureFeedbackKind ToPersistenceFeedbackKind(
        CapturePersistenceFailureKind kind)
        => kind switch
        {
            CapturePersistenceFailureKind.AccessDenied => CaptureFeedbackKind.SaveAccessDenied,
            CapturePersistenceFailureKind.StorageFull => CaptureFeedbackKind.StorageFull,
            _ => CaptureFeedbackKind.SaveFailed
        };

    public bool TryBeginShutdown()
    {
        if (_activityGate.TryBeginShutdown())
        {
            return true;
        }

        if (_recordingSessionGate.IsActive)
        {
            StartupDiagnostics.WriteLine(
                "Shutdown requested during screen recording; requesting a graceful recording stop.");
            _ = _recordingSessionGate.RequestStop();
        }

        StartupDiagnostics.WriteLine(
            "Shutdown request deferred until the active capture finishes cleanup.");
        ShowCaptureFeedback(CaptureFeedbackKind.ShutdownBlocked);
        return false;
    }

    private bool TryBeginCapture()
    {
        if (_activityGate.TryBeginCapture())
        {
            return true;
        }

        StartupDiagnostics.WriteLine(
            _activityGate.IsCaptureInProgress
                ? "Capture request ignored because another capture is already active."
                : "Capture request ignored because application shutdown has started.");
        ShowCaptureFeedback(CaptureFeedbackKind.Busy);
        return false;
    }

    private void ShowCaptureFeedback(CaptureFeedbackKind kind)
    {
        CloseCaptureFeedback();

        var feedback = new CaptureFeedbackWindow(
            kind,
            _capturePreferencesService.Current.LanguageCode);
        _feedbackWindow = feedback;
        feedback.Closed += (_, _) =>
        {
            if (ReferenceEquals(_feedbackWindow, feedback))
            {
                _feedbackWindow = null;
            }
        };
        feedback.Activate();
    }

    private void CloseCaptureFeedback()
    {
        var feedback = _feedbackWindow;
        _feedbackWindow = null;
        if (feedback is null)
        {
            return;
        }

        try
        {
            feedback.Close();
        }
        catch (InvalidOperationException)
        {
            // The feedback window may already be closing through user chrome.
        }
    }

    private void EndCapture()
    {
        _activityGate.EndCapture();
        if (!_activityGate.IsShutdownRequested)
        {
            return;
        }

        StartupDiagnostics.WriteLine(
            "Active capture cleanup completed; continuing deferred shutdown.");
        Close();
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
}
