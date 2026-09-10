using Snapvere.Capture;
using Snapvere.Capture.Region;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;

namespace Snapvere.Application.Capture;

public sealed record RegionCaptureSession(
    DisplayDescriptor Display,
    CaptureFrame FrozenFrame);

/// <summary>
/// Coordinates a freeze-frame region capture. UI owns only interaction and
/// selection presentation; this workflow owns display selection, frame
/// capture, pixel-space mapping, cropping, annotations and durable PNG persistence.
/// </summary>
public sealed class RegionCaptureWorkflow
{
    private readonly IDisplayDiscovery _displayDiscovery;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly CaptureFileWriter _fileWriter;
    private readonly CapturePreferencesService? _preferences;

    public RegionCaptureWorkflow(
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

    public async Task<RegionCaptureSession> PreparePrimaryDisplayAsync(
        bool includeCursor = false,
        CancellationToken cancellationToken = default)
    {
        var display = GetPrimaryDisplay();
        var effectiveIncludeCursor = includeCursor || (_preferences?.Current.IncludeCursorOnCapture ?? false);
        var frame = await _screenCaptureService
            .CaptureDisplayAsync(display, effectiveIncludeCursor, cancellationToken)
            .ConfigureAwait(false);

        frame.Validate();

        var bounds = display.Bounds.Normalize();
        if (frame.Size.Width != bounds.Width || frame.Size.Height != bounds.Height)
        {
            throw new InvalidOperationException(
                "The frozen capture frame no longer matches the selected display. The display configuration may have changed.");
        }

        return new RegionCaptureSession(display, frame);
    }

    public CaptureFrame CreateSelectionFrame(
        RegionCaptureSession session,
        PixelRect desktopSelection,
        IReadOnlyList<CaptureAnnotation>? annotations = null)
    {
        ArgumentNullException.ThrowIfNull(session);

        var displayBounds = session.Display.Bounds.Normalize();
        var selection = RegionSelectionGeometry.Clamp(desktopSelection, displayBounds);
        if (selection.IsEmpty)
        {
            throw new ArgumentException("Region selection must contain at least one pixel.", nameof(desktopSelection));
        }

        var localRegion = new PixelRect(
            checked(selection.X - displayBounds.X),
            checked(selection.Y - displayBounds.Y),
            selection.Width,
            selection.Height);

        var cropped = CaptureFrameCropper.Crop(session.FrozenFrame, localRegion);
        return CaptureFrameAnnotator.Apply(cropped, annotations);
    }

    public async Task<CaptureSaveResult> SaveSelectionAsync(
        RegionCaptureSession session,
        PixelRect desktopSelection,
        IReadOnlyList<CaptureAnnotation>? annotations = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var frame = CreateSelectionFrame(session, desktopSelection, annotations);
        return await _fileWriter.SavePngAsync(frame, cancellationToken).ConfigureAwait(false);
    }

    private DisplayDescriptor GetPrimaryDisplay()
    {
        var displays = _displayDiscovery.GetDisplays();
        if (displays.Count == 0)
        {
            throw new InvalidOperationException("SNAPVERE could not find an active display.");
        }

        return displays.FirstOrDefault(candidate => candidate.IsPrimary) ?? displays[0];
    }
}
