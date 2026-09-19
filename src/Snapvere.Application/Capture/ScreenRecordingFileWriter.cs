using Snapvere.Capture;
using Snapvere.Domain.Capture;

namespace Snapvere.Application.Capture;

public sealed record ScreenRecordingSaveResult(
    string FilePath,
    PixelSize SourceSize,
    PixelSize EncodedSize,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt)
{
    public TimeSpan Duration => CompletedAt - StartedAt;
}

public sealed class ScreenRecordingFileWriter
{
    private const int MaximumFileNameAttempts = 1000;
    private static readonly TimeSpan StaleTemporaryFileAge = TimeSpan.FromHours(24);

    private readonly IScreenRecordingService _recordingService;
    private readonly CapturePathProvider _pathProvider;
    private readonly TimeProvider _timeProvider;

    public ScreenRecordingFileWriter(
        IScreenRecordingService recordingService,
        CapturePathProvider pathProvider,
        TimeProvider timeProvider)
    {
        _recordingService = recordingService ?? throw new ArgumentNullException(nameof(recordingService));
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ScreenRecordingSaveResult> RecordAsync(
        DisplayDescriptor display,
        bool includeCursor,
        CancellationToken stopToken)
    {
        ArgumentNullException.ThrowIfNull(display);

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
            throw new CapturePersistenceException(
                CapturePersistenceFailureKind.WriteFailed,
                exception);
        }

        CleanupStaleTemporaryFiles(directory, _timeProvider.GetUtcNow());

        var timestamp = _timeProvider.GetLocalNow();
        var temporaryPath = Path.Combine(
            directory,
            $".{BuildFileName(timestamp)}.{Guid.NewGuid():N}.tmp");

        try
        {
            ScreenRecordingSessionResult session;
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 128 * 1024,
                useAsync: true))
            {
                session = await _recordingService
                    .RecordDisplayAsync(display, includeCursor, stream, stopToken)
                    .ConfigureAwait(false);
            }

            var finalPath = PublishTemporaryFile(temporaryPath, directory, timestamp);
            return new ScreenRecordingSaveResult(
                finalPath,
                session.SourceSize,
                session.EncodedSize,
                session.StartedAt,
                session.CompletedAt);
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
    }

    public static string BuildFileName(DateTimeOffset timestamp, int counter = 0)
    {
        if (counter < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(counter));
        }

        var baseName = $"SNAPVERE_Record_{timestamp:yyyy-MM-dd_HHmmss}";
        return counter == 0
            ? $"{baseName}.mp4"
            : $"{baseName}_{counter:D3}.mp4";
    }

    private static string PublishTemporaryFile(
        string temporaryPath,
        string directory,
        DateTimeOffset timestamp)
    {
        for (var counter = 0; counter < MaximumFileNameAttempts; counter++)
        {
            var finalPath = Path.Combine(directory, BuildFileName(timestamp, counter));
            try
            {
                File.Move(temporaryPath, finalPath, overwrite: false);
                return finalPath;
            }
            catch (IOException) when (File.Exists(finalPath))
            {
            }
        }

        throw new IOException("SNAPVERE could not allocate a unique screen-recording file name.");
    }

    private static void CleanupStaleTemporaryFiles(string directory, DateTimeOffset utcNow)
    {
        try
        {
            var cutoffUtc = utcNow.UtcDateTime - StaleTemporaryFileAge;
            foreach (var path in Directory.EnumerateFiles(
                         directory,
                         ".SNAPVERE_Record_*.tmp",
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
                }
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
        }
    }

    private static bool IsOwnedTemporaryFileName(string fileName)
    {
        const string prefix = ".SNAPVERE_Record_";
        const string marker = ".mp4.";
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
        return tokenLength == 32 &&
               Guid.TryParseExact(fileName.AsSpan(tokenStart, tokenLength), "N", out _);
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
        }
    }
}
