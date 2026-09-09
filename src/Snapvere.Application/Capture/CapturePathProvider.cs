namespace Snapvere.Application.Capture;

public sealed record CaptureSaveResult(
    string FilePath,
    int Width,
    int Height,
    DateTimeOffset CapturedAt);

public sealed class CapturePathProvider
{
    private const string ProductFolderName = "SNAPVERE";
    private readonly string? _overrideDirectory;

    public CapturePathProvider(string? overrideDirectory = null)
    {
        _overrideDirectory = string.IsNullOrWhiteSpace(overrideDirectory)
            ? null
            : Path.GetFullPath(overrideDirectory);
    }

    public string GetDefaultCaptureDirectory()
    {
        if (_overrideDirectory is not null)
        {
            return _overrideDirectory;
        }

        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (string.IsNullOrWhiteSpace(pictures))
        {
            pictures = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (string.IsNullOrWhiteSpace(pictures))
        {
            throw new InvalidOperationException("Windows did not provide a user Pictures or profile directory.");
        }

        return Path.Combine(pictures, ProductFolderName);
    }

    public static string BuildFileName(DateTimeOffset timestamp, int counter = 0)
    {
        if (counter < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(counter));
        }

        var baseName = $"SNAPVERE_{timestamp:yyyy-MM-dd_HHmmss}";
        return counter == 0
            ? $"{baseName}.png"
            : $"{baseName}_{counter:D3}.png";
    }

    public static string GetAvailablePath(string directory, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        for (var counter = 0; counter <= 999; counter++)
        {
            var candidate = Path.Combine(directory, BuildFileName(timestamp, counter));
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("SNAPVERE could not allocate a unique capture file name.");
    }
}
