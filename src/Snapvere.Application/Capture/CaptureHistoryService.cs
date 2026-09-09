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

        foreach (var path in Directory.EnumerateFiles(directory, "SNAPVERE_*.png", SearchOption.TopDirectoryOnly))
        {
            if (!TryReadCapture(path, out var item))
            {
                continue;
            }

            newest.Enqueue(item, item.ModifiedAt.UtcTicks);
            if (newest.Count > limit)
            {
                _ = newest.Dequeue();
            }
        }

        return newest.UnorderedItems
            .Select(entry => entry.Element)
            .OrderByDescending(item => item.ModifiedAt)
            .ThenByDescending(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool DeleteCapture(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var safePath = GetValidatedCapturePath(filePath);
        if (!File.Exists(safePath))
        {
            return false;
        }

        File.Delete(safePath);
        return true;
    }

    public string GetCaptureDirectory()
        => _pathProvider.GetDefaultCaptureDirectory();

    private static bool TryReadCapture(string path, out CaptureHistoryItem item)
    {
        try
        {
            var file = new FileInfo(path);
            file.Refresh();
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
        catch (IOException)
        {
            item = null!;
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            item = null!;
            return false;
        }
    }

    private string GetValidatedCapturePath(string filePath)
    {
        var root = Path.GetFullPath(_pathProvider.GetDefaultCaptureDirectory());
        var candidate = Path.GetFullPath(filePath);
        var relative = Path.GetRelativePath(root, candidate);

        if (relative == "." ||
            relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            relative == ".." ||
            Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException("The requested history item is outside the SNAPVERE capture directory.");
        }

        var fileName = Path.GetFileName(candidate);
        if (!fileName.StartsWith("SNAPVERE_", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetExtension(fileName), ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only SNAPVERE PNG capture files can be managed by this history service.");
        }

        return candidate;
    }
}
