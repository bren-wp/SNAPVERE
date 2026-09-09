using Snapvere.Capture;
using Snapvere.Imaging;

namespace Snapvere.Application.Capture;

public sealed class ScreenCaptureWorkflow
{
    private readonly IDisplayDiscovery _displayDiscovery;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly PngCaptureEncoder _pngEncoder;
    private readonly CapturePathProvider _pathProvider;
    private readonly TimeProvider _timeProvider;

    public ScreenCaptureWorkflow(
        IDisplayDiscovery displayDiscovery,
        IScreenCaptureService screenCaptureService,
        PngCaptureEncoder pngEncoder,
        CapturePathProvider pathProvider,
        TimeProvider timeProvider)
    {
        _displayDiscovery = displayDiscovery ?? throw new ArgumentNullException(nameof(displayDiscovery));
        _screenCaptureService = screenCaptureService ?? throw new ArgumentNullException(nameof(screenCaptureService));
        _pngEncoder = pngEncoder ?? throw new ArgumentNullException(nameof(pngEncoder));
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CaptureSaveResult> CapturePrimaryDisplayToDefaultFolderAsync(
        bool includeCursor,
        CancellationToken cancellationToken = default)
    {
        var displays = _displayDiscovery.GetDisplays();
        if (displays.Count == 0)
        {
            throw new InvalidOperationException("SNAPVERE could not find an active display.");
        }

        var display = displays.FirstOrDefault(candidate => candidate.IsPrimary) ?? displays[0];
        var frame = await _screenCaptureService
            .CaptureDisplayAsync(display, includeCursor, cancellationToken)
            .ConfigureAwait(false);

        frame.Validate();

        var directory = _pathProvider.GetDefaultCaptureDirectory();
        Directory.CreateDirectory(directory);

        var timestamp = _timeProvider.GetLocalNow();
        var finalPath = CapturePathProvider.GetAvailablePath(directory, timestamp);
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(finalPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                useAsync: true))
            {
                await _pngEncoder.EncodeAsync(frame, stream, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, finalPath, overwrite: false);
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }

        return new CaptureSaveResult(
            finalPath,
            frame.Size.Width,
            frame.Size.Height,
            frame.CapturedAt);
    }

    private static void TryDeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup. A future startup temp cleanup pass handles leftovers.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup. Do not hide the original capture failure.
        }
    }
}
