using Snapvere.Capture;

namespace Snapvere.Application.Capture;

public sealed class ScreenCaptureWorkflow
{
    private readonly IDisplayDiscovery _displayDiscovery;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly CaptureFileWriter _fileWriter;
    private readonly CapturePreferencesService? _preferences;

    public ScreenCaptureWorkflow(
        IDisplayDiscovery displayDiscovery,
        IScreenCaptureService screenCaptureService,
        CaptureFileWriter fileWriter,
        CapturePreferencesService? preferences = null)
    {
        _displayDiscovery = displayDiscovery ?? throw new ArgumentNullException(nameof(displayDiscovery));
        _screenCaptureService = screenCaptureService ?? throw new ArgumentNullException(nameof(screenCaptureService));
        _fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
        _preferences = preferences;
    }

    public async Task<CaptureSaveResult> CapturePrimaryDisplayToDefaultFolderAsync(
        bool includeCursor,
        CancellationToken cancellationToken = default)
    {
        var display = GetPrimaryDisplay();
        var effectiveIncludeCursor = includeCursor || (_preferences?.Current.IncludeCursorOnCapture ?? false);
        var frame = await _screenCaptureService
            .CaptureDisplayAsync(display, effectiveIncludeCursor, cancellationToken)
            .ConfigureAwait(false);

        frame.Validate();
        return await _fileWriter.SavePngAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    private Snapvere.Domain.Capture.DisplayDescriptor GetPrimaryDisplay()
    {
        var displays = _displayDiscovery.GetDisplays();
        if (displays.Count == 0)
        {
            throw new InvalidOperationException("SNAPVERE could not find an active display.");
        }

        return displays.FirstOrDefault(candidate => candidate.IsPrimary) ?? displays[0];
    }
}
