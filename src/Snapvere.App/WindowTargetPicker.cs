using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Capture.Windows;

namespace Snapvere.App;

/// <summary>
/// Coordinates one runtime-safe picker overlay per monitor. Desktop frames and
/// the capturable-window Z-order are frozen before overlays appear so hover
/// targeting remains stable even while SNAPVERE owns the topmost surfaces.
/// </summary>
public sealed class WindowTargetPicker
{
    private readonly IDisplayDiscovery _displayDiscovery;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly WindowCaptureWorkflow _windowCaptureWorkflow;

    public WindowTargetPicker(
        IDisplayDiscovery displayDiscovery,
        IScreenCaptureService screenCaptureService,
        WindowCaptureWorkflow windowCaptureWorkflow)
    {
        _displayDiscovery = displayDiscovery ?? throw new ArgumentNullException(nameof(displayDiscovery));
        _screenCaptureService = screenCaptureService ?? throw new ArgumentNullException(nameof(screenCaptureService));
        _windowCaptureWorkflow = windowCaptureWorkflow ?? throw new ArgumentNullException(nameof(windowCaptureWorkflow));
    }

    public async Task<WindowDescriptor?> PickAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var displays = _displayDiscovery.GetDisplays();
        if (displays.Count == 0)
        {
            throw new InvalidOperationException("SNAPVERE could not find an active display for Window Capture.");
        }

        var windows = _windowCaptureWorkflow.GetAvailableWindows();
        if (windows.Count == 0)
        {
            throw new InvalidOperationException("SNAPVERE could not find a visible window to capture.");
        }

        var frozenFrames = new Dictionary<string, CaptureFrame>(StringComparer.Ordinal);
        foreach (var display in displays)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var frame = await _screenCaptureService.CaptureDisplayAsync(
                display,
                includeCursor: false,
                cancellationToken);
            frame.Validate();
            frozenFrames.Add(display.Id, frame);
        }

        var completion = new TaskCompletionSource<WindowDescriptor?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var overlays = new List<WindowTargetOverlayWindow>(displays.Count);

        void UpdateTarget(WindowDescriptor? target)
        {
            foreach (var overlay in overlays)
            {
                overlay.SetTarget(target);
            }
        }

        void SelectTarget(WindowDescriptor target)
            => completion.TrySetResult(target);

        void Cancel()
            => completion.TrySetResult(null);

        try
        {
            foreach (var display in displays)
            {
                var overlay = new WindowTargetOverlayWindow(
                    display,
                    frozenFrames[display.Id],
                    windows,
                    UpdateTarget,
                    SelectTarget,
                    Cancel);
                overlays.Add(overlay);
            }

            foreach (var overlay in overlays)
            {
                overlay.Show();
            }

            using var cancellationRegistration = cancellationToken.Register(
                () => completion.TrySetCanceled(cancellationToken));
            return await completion.Task;
        }
        finally
        {
            foreach (var overlay in overlays)
            {
                overlay.CloseSafely();
            }
        }
    }
}
