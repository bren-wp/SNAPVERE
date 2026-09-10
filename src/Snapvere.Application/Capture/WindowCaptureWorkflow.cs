using Snapvere.Capture;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;

namespace Snapvere.Application.Capture;

public sealed class WindowCaptureWorkflow
{
    private readonly IWindowDiscovery _windowDiscovery;
    private readonly IWindowCaptureService _windowCaptureService;
    private readonly CaptureFileWriter _fileWriter;
    private readonly CapturePreferencesService? _preferences;

    public WindowCaptureWorkflow(
        IWindowDiscovery windowDiscovery,
        IWindowCaptureService windowCaptureService,
        CaptureFileWriter fileWriter,
        CapturePreferencesService? preferences = null)
    {
        _windowDiscovery = windowDiscovery ?? throw new ArgumentNullException(nameof(windowDiscovery));
        _windowCaptureService = windowCaptureService ?? throw new ArgumentNullException(nameof(windowCaptureService));
        _fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
        _preferences = preferences;
    }

    public IReadOnlyList<WindowDescriptor> GetAvailableWindows()
        => _windowDiscovery.GetWindows();

    public WindowDescriptor? TryGetWindowAtPoint(PixelPoint point)
        => _windowDiscovery.TryGetWindowAtPoint(point);

    public async Task<CaptureSaveResult> CaptureWindowToDefaultFolderAsync(
        WindowDescriptor window,
        bool includeCursor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);
        cancellationToken.ThrowIfCancellationRequested();

        var effectiveIncludeCursor = includeCursor || (_preferences?.Current.IncludeCursorOnCapture ?? false);
        var frame = await _windowCaptureService
            .CaptureWindowAsync(window, effectiveIncludeCursor, cancellationToken)
            .ConfigureAwait(false);
        frame.Validate();

        return await _fileWriter
            .SavePngAsync(frame, cancellationToken)
            .ConfigureAwait(false);
    }
}
