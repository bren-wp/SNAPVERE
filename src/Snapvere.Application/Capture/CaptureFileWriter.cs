using Snapvere.Capture;
using Snapvere.Imaging;

namespace Snapvere.Application.Capture;

/// <summary>
/// Writes capture frames to the local SNAPVERE capture folder using a
/// temp-file + atomic move sequence so partially written images never appear
/// as completed captures.
/// </summary>
public sealed class CaptureFileWriter
{
    private readonly PngCaptureEncoder _pngEncoder;
    private readonly CapturePathProvider _pathProvider;
    private readonly TimeProvider _timeProvider;

    public CaptureFileWriter(
        PngCaptureEncoder pngEncoder,
        CapturePathProvider pathProvider,
        TimeProvider timeProvider)
    {
        _pngEncoder = pngEncoder ?? throw new ArgumentNullException(nameof(pngEncoder));
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CaptureSaveResult> SavePngAsync(
        CaptureFrame frame,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(frame);
        frame.Validate();
        cancellationToken.ThrowIfCancellationRequested();

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
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Best-effort cleanup. Never replace the original capture failure
            // with a secondary cleanup error; a future cleanup pass can remove
            // any leftover temporary file.
        }
    }
}
