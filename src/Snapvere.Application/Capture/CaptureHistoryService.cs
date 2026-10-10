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

        if (bytes < 1024L * 1024L * 1024L)
        {
            return $"{bytes / (1024d * 1024d):F1} MB";
        }

        if (bytes < 1024L * 1024L * 1024L * 1024L)
        {
            return $"{bytes / (1024d * 1024d * 1024d):F1} GB";
        }

        return $"{bytes / (1024d * 1024d * 1024d * 1024d):F1} TB";
    }
}

/// <summary>
/// Local filesystem-backed capture history foundation. No database, network
/// service or telemetry is required to discover recent SNAPVERE captures.
/// </summary>
public sealed class CaptureHistoryService
{
    // The newest capture list is bounded, but files can share the exact same
    // timestamp (especially after copy or restore). Match the final display
    // ordering at the queue boundary so equal-time items are deterministic.
    private static readonly IComparer<(long ModifiedUtcTicks, string FileName)> RecentCapturePriorityComparer =
        Comparer<(long ModifiedUtcTicks, string FileName)>.Create((left, right) =>
        {
            var timestampOrder = left.ModifiedUtcTicks.CompareTo(right.ModifiedUtcTicks);
            return timestampOrder != 0
                ? timestampOrder
                : StringComparer.OrdinalIgnoreCase.Compare(left.FileName, right.FileName);
        });

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

        var newest = new PriorityQueue<CaptureHistoryItem, (long ModifiedUtcTicks, string FileName)>(
            RecentCapturePriorityComparer);

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

                newest.Enqueue(item, (item.ModifiedAt.UtcTicks, item.FileName));
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

    /// <summary>
    /// Revalidate a recent item at click time, because files in Pictures can
    /// disappear or be replaced after the history list was generated.
    /// Only actual top-level capture files are eligible for Open/Copy Path.
    /// </summary>
    public bool IsCurrentCaptureFile(CaptureHistoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        try
        {
            var captureDirectory = Path.GetFullPath(_pathProvider.GetDefaultCaptureDirectory());
            var filePath = Path.GetFullPath(item.FilePath);
            if (!string.Equals(Path.GetDirectoryName(filePath), captureDirectory, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFileName(filePath), item.FileName, StringComparison.OrdinalIgnoreCase) ||
                !item.FileName.StartsWith("SNAPVERE_", StringComparison.OrdinalIgnoreCase) ||
                !IsSupportedCaptureFile(item.FileName))
            {
                return false;
            }

            // A file can be overwritten at the same path after the Recent list
            // was built. Reject stale entries instead of opening or copying
            // a different file behind a previously displayed history item.
            return TryReadCapture(new FileInfo(filePath), out var current) &&
                   current.FileSizeBytes == item.FileSizeBytes &&
                   current.ModifiedAt == item.ModifiedAt;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or
            System.Security.SecurityException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

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
            if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                // Do not expose files outside the capture folder through
                // links named like SNAPVERE screenshots or videos.
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
