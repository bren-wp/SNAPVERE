using Snapvere.Capture;
using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;

namespace Snapvere.Application.Capture;

public sealed class WindowCaptureWorkflow
{
    private readonly IWindowDiscovery _windowDiscovery;
    private readonly IWindowCaptureService _windowCaptureService;
    private readonly CaptureFileWriter _fileWriter;

    public WindowCaptureWorkflow(
        IWindowDiscovery windowDiscovery,
        IWindowCaptureService windowCaptureService,
        CaptureFileWriter fileWriter)
    {
        _windowDiscovery = windowDiscovery ?? throw new ArgumentNullException(nameof(windowDiscovery));
        _windowCaptureService = windowCaptureService ?? throw new ArgumentNullException(nameof(windowCaptureService));
        _fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
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

        var frame = await _windowCaptureService
            .CaptureWindowAsync(window, includeCursor, cancellationToken)
            .ConfigureAwait(false);
        frame.Validate();

        return await _fileWriter
            .SavePngAsync(frame, cancellationToken)
            .ConfigureAwait(false);
    }
}
