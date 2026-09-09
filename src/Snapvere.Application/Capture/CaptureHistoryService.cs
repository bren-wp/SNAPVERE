namespace Snapvere.Application.Capture;

public sealed record CaptureHistoryItem(
    string FilePath,
    string FileName,
    DateTimeOffset ModifiedAt,
    long FileSizeBytes)
{
    public string MetadataText => $"{ModifiedAt.LocalDateTime:g} · {FormatFileSize(FileSizeBytes)}";

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

        return Directory
            .EnumerateFiles(directory, "SNAPVERE_*.png", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .Where(file => file.Exists)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(file => new CaptureHistoryItem(
                file.FullName,
                file.Name,
                new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero),
                file.Length))
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

        if (!string.Equals(Path.GetExtension(candidate), ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only SNAPVERE PNG capture files can be managed by this history service.");
        }

        return candidate;
    }
}
