using Snapvere.Capture;

namespace Snapvere.Application.Capture;

public sealed class ScreenRecordingWorkflow
{
    private readonly IDisplayDiscovery _displayDiscovery;
    private readonly ScreenRecordingFileWriter _fileWriter;
    private readonly CapturePreferencesService? _preferences;

    public ScreenRecordingWorkflow(
        IDisplayDiscovery displayDiscovery,
        ScreenRecordingFileWriter fileWriter,
        CapturePreferencesService? preferences = null)
    {
        _displayDiscovery = displayDiscovery ?? throw new ArgumentNullException(nameof(displayDiscovery));
        _fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
        _preferences = preferences;
    }

    public Task<ScreenRecordingSaveResult> RecordPrimaryDisplayAsync(
        bool includeCursor,
        CancellationToken stopToken)
    {
        var displays = _displayDiscovery.GetDisplays();
        if (displays.Count == 0)
        {
            throw new InvalidOperationException("SNAPVERE could not find an active display.");
        }

        var display = displays.FirstOrDefault(candidate => candidate.IsPrimary) ?? displays[0];
        var effectiveIncludeCursor =
            includeCursor || (_preferences?.Current.IncludeCursorOnCapture ?? false);
        return _fileWriter.RecordAsync(display, effectiveIncludeCursor, stopToken);
    }
}
