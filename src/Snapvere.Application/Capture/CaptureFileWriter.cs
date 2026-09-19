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
    private const int MaximumFileNameAttempts = 1000;
    private static readonly TimeSpan StaleTemporaryFileAge = TimeSpan.FromHours(24);

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

        string directory;
        try
        {
            directory = _pathProvider.GetDefaultCaptureDirectory();
            Directory.CreateDirectory(directory);
        }
        catch (Exception exception) when (
            CapturePersistenceFailurePolicy.TryClassify(exception, out _))
        {
            throw CapturePersistenceFailurePolicy.Wrap(exception);
        }
        catch (InvalidOperationException exception)
        {
            // Windows did not provide a usable local capture directory. This
            // is a persistence-location failure, not a capture-engine failure.
            throw new CapturePersistenceException(
                CapturePersistenceFailureKind.WriteFailed,
                exception);
        }

        CleanupStaleTemporaryFiles(directory, _timeProvider.GetUtcNow());

        var timestamp = _timeProvider.GetLocalNow();
        var temporaryPath = Path.Combine(
            directory,
            $".{CapturePathProvider.BuildFileName(timestamp)}.{Guid.NewGuid():N}.tmp");
        string? finalPath = null;

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

            // The move is the commit boundary. Allocate the visible file name
            // at commit time rather than before encoding so simultaneous
            // captures with the same timestamp cannot race on one candidate.
            finalPath = PublishTemporaryFile(
                temporaryPath,
                directory,
                timestamp,
                cancellationToken);
        }
        catch (Exception exception) when (
            CapturePersistenceFailurePolicy.TryClassify(exception, out _))
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw CapturePersistenceFailurePolicy.Wrap(exception);
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

    private static string PublishTemporaryFile(
        string temporaryPath,
        string directory,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        for (var counter = 0; counter < MaximumFileNameAttempts; counter++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var finalPath = Path.Combine(
                directory,
                CapturePathProvider.BuildFileName(timestamp, counter));

            try
            {
                File.Move(temporaryPath, finalPath, overwrite: false);
                return finalPath;
            }
            catch (IOException) when (File.Exists(finalPath))
            {
                // Another capture/process won this filename after our previous
                // check. Retry the next deterministic suffix without
                // re-encoding or exposing a partial file.
            }
        }

        throw new IOException("SNAPVERE could not allocate a unique capture file name.");
    }

    private static void CleanupStaleTemporaryFiles(string directory, DateTimeOffset utcNow)
    {
        try
        {
            var cutoffUtc = utcNow.UtcDateTime - StaleTemporaryFileAge;
            foreach (var path in Directory.EnumerateFiles(
                         directory,
                         ".SNAPVERE_*.tmp",
                         SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var fileName = Path.GetFileName(path);
                    if (!IsOwnedTemporaryFileName(fileName) ||
                        File.GetLastWriteTimeUtc(path) > cutoffUtc)
                    {
                        continue;
                    }

                    File.Delete(path);
                }
                catch (Exception exception) when (
                    exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                {
                    // Cleanup is opportunistic and must never block a new capture.
                }
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Enumerating stale temp files is best effort for the same reason.
        }
    }

    private static bool IsOwnedTemporaryFileName(string fileName)
    {
        const string prefix = ".SNAPVERE_";
        const string marker = ".png.";
        const string suffix = ".tmp";

        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var markerIndex = fileName.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < prefix.Length)
        {
            return false;
        }

        var tokenStart = markerIndex + marker.Length;
        var tokenLength = fileName.Length - tokenStart - suffix.Length;
        if (tokenLength != 32)
        {
            return false;
        }

        return Guid.TryParseExact(fileName.AsSpan(tokenStart, tokenLength), "N", out _);
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
            // with a secondary cleanup error; a later capture pass removes
            // stale SNAPVERE-owned temporary files.
        }
    }
}
