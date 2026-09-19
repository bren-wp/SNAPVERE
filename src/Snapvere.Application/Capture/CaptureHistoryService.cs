namespace Snapvere.Application.Capture;

public sealed record CaptureHistoryItem(
    string FilePath,
    string FileName,
    DateTimeOffset ModifiedAt,
    long FileSizeBytes)
{
    public string MetadataText => $"{ModifiedAt.LocalDateTime:g} · {FormatFileSize(FileSizeBytes)}";

    public override string ToString()
        => $"{FileName}    {MetadataText}";

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024L * 1024L)
        {
            return $"{bytes / 1024d:F1} KB";
        }

        return $"{bytes / (1024d * 1024d):F1} MB";
    }
}

/// <summary>
/// Local filesystem-backed capture history foundation. No database, network
/// service or telemetry is required to discover recent SNAPVERE captures.
/// </summary>
public sealed class CaptureHistoryService
{
    private readonly CapturePathProvider _pathProvider;

    public CaptureHistoryService(CapturePathProvider pathProvider)
    {
        _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
    }

    public IReadOnlyList<CaptureHistoryItem> GetRecentCaptures(int limit = 12)
    {
        if (limit < 1 || limit > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        var directory = _pathProvider.GetDefaultCaptureDirectory();
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var newest = new PriorityQueue<CaptureHistoryItem, long>();

        try
        {
            var directoryInfo = new DirectoryInfo(directory);
            foreach (var file in directoryInfo.EnumerateFiles("SNAPVERE_*.*", SearchOption.TopDirectoryOnly))
            {
                if (!IsSupportedCaptureFile(file.Name) ||
                    !TryReadCapture(file, out var item))
                {
                    continue;
                }

                newest.Enqueue(item, item.ModifiedAt.UtcTicks);
                if (newest.Count > limit)
                {
                    _ = newest.Dequeue();
                }
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException or System.Security.SecurityException)
        {
            // The Pictures directory may disappear, become unavailable or be
            // blocked by policy while it is being enumerated. Preserve any
            // metadata already collected instead of taking down the UI.
        }

        return newest.UnorderedItems
            .Select(entry => entry.Element)
            .OrderByDescending(item => item.ModifiedAt)
            .ThenByDescending(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string GetCaptureDirectory()
        => _pathProvider.GetDefaultCaptureDirectory();

    private static bool IsSupportedCaptureFile(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadCapture(FileInfo file, out CaptureHistoryItem item)
    {
        try
        {
            if (!file.Exists)
            {
                item = null!;
                return false;
            }

            item = new CaptureHistoryItem(
                file.FullName,
                file.Name,
                new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero),
                file.Length);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            item = null!;
            return false;
        }
    }
}
