using Snapvere.Capture;
using Snapvere.Imaging;

namespace Snapvere.Application.Capture;

/// <summary>
/// Writes capture frames as PNG files using a temp-file + atomic move sequence
/// so partially written images never appear as completed captures. Callers can
/// use the default SNAPVERE capture directory or an explicit user-selected path.
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

    public Task<CaptureSaveResult> SavePngAsync(
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
        return SavePngCoreAsync(frame, finalPath, overwrite: false, cancellationToken);
    }

    public Task<CaptureSaveResult> SavePngAsync(
        CaptureFrame frame,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(frame);
        frame.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        cancellationToken.ThrowIfCancellationRequested();

        var finalPath = Path.GetFullPath(destinationPath);
        if (!string.Equals(Path.GetExtension(finalPath), ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("SNAPVERE capture destinations must use the .png extension.", nameof(destinationPath));
        }

        var directory = Path.GetDirectoryName(finalPath)
            ?? throw new ArgumentException("The capture destination must include a parent directory.", nameof(destinationPath));
        Directory.CreateDirectory(directory);

        // A user-selected FileSavePicker path may intentionally point at an
        // existing file after Windows has already confirmed replacement.
        return SavePngCoreAsync(frame, finalPath, overwrite: true, cancellationToken);
    }

    private async Task<CaptureSaveResult> SavePngCoreAsync(
        CaptureFrame frame,
        string finalPath,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(finalPath)
            ?? throw new InvalidOperationException("The SNAPVERE capture path has no parent directory.");
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

            File.Move(temporaryPath, finalPath, overwrite);
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
            // Best-effort cleanup. A future startup cleanup pass handles leftovers.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup. Do not hide the original capture failure.
        }
    }
}
