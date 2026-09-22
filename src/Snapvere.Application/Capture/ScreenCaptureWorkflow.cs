using Snapvere.Capture;

namespace Snapvere.Application.Capture;

public sealed class ScreenCaptureWorkflow
{
    private readonly IDisplayDiscovery _displayDiscovery;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly CaptureFileWriter _fileWriter;
    private readonly CapturePreferencesService? _preferences;
    private readonly ICaptureDisplaySelector? _displaySelector;

    public ScreenCaptureWorkflow(
        IDisplayDiscovery displayDiscovery,
        IScreenCaptureService screenCaptureService,
        CaptureFileWriter fileWriter,
        CapturePreferencesService? preferences = null,
        ICaptureDisplaySelector? displaySelector = null)
    {
        _displayDiscovery = displayDiscovery ?? throw new ArgumentNullException(nameof(displayDiscovery));
        _screenCaptureService = screenCaptureService ?? throw new ArgumentNullException(nameof(screenCaptureService));
        _fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
        _preferences = preferences;
        _displaySelector = displaySelector;
    }

    public Task<CaptureSaveResult> CaptureInteractiveDisplayToDefaultFolderAsync(
        bool includeCursor,
        CancellationToken cancellationToken = default)
        => CaptureDisplayToDefaultFolderAsync(
            GetInteractiveDisplay(),
            includeCursor,
            cancellationToken);

    public Task<CaptureSaveResult> CapturePrimaryDisplayToDefaultFolderAsync(
        bool includeCursor,
        CancellationToken cancellationToken = default)
        => CaptureDisplayToDefaultFolderAsync(
            GetPrimaryDisplay(),
            includeCursor,
            cancellationToken);

    private async Task<CaptureSaveResult> CaptureDisplayToDefaultFolderAsync(
        Snapvere.Domain.Capture.DisplayDescriptor display,
        bool includeCursor,
        CancellationToken cancellationToken)
    {
        var effectiveIncludeCursor = includeCursor || (_preferences?.Current.IncludeCursorOnCapture ?? false);
        var frame = await _screenCaptureService
            .CaptureDisplayAsync(display, effectiveIncludeCursor, cancellationToken)
            .ConfigureAwait(false);

        frame.Validate();
        return await _fileWriter.SavePngAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    private Snapvere.Domain.Capture.DisplayDescriptor GetInteractiveDisplay()
    {
        var displays = _displayDiscovery.GetDisplays();
        return _displaySelector?.SelectDisplay(displays)
            ?? CaptureDisplaySelectionPolicy.SelectPrimary(displays);
    }

    private Snapvere.Domain.Capture.DisplayDescriptor GetPrimaryDisplay()
        => CaptureDisplaySelectionPolicy.SelectPrimary(_displayDiscovery.GetDisplays());
}
