namespace Snapvere.Packaging;

/// <summary>
/// Shared installer path and marker validation used by Setup before mutating
/// user-writable locations.
/// </summary>
public static class InstallSafetyPolicy
{
    public static string NormalizeProductDirectory(string selectedDirectory, string productDirectoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(productDirectoryName);

        var fullPath = Path.GetFullPath(selectedDirectory.Trim());
        var trimmedPath = Path.TrimEndingDirectorySeparator(fullPath);
        var leafName = Path.GetFileName(trimmedPath);

        if (string.Equals(leafName, productDirectoryName, StringComparison.OrdinalIgnoreCase))
        {
            return trimmedPath;
        }

        return Path.Combine(trimmedPath, productDirectoryName);
    }

    public static bool HasExactMarkerHeader(string markerText, string expectedHeader)
    {
        ArgumentNullException.ThrowIfNull(markerText);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedHeader);

        using var reader = new StringReader(markerText);
        return string.Equals(reader.ReadLine(), expectedHeader, StringComparison.Ordinal);
    }

    public static void EnsureExistingDirectoryChainHasNoReparsePoints(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        var current = new DirectoryInfo(Path.GetFullPath(directoryPath));
        while (current is not null)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException(
                    "SNAPVERE cannot install through a symbolic link or reparse-point directory.");
            }

            current = current.Parent;
        }
    }
}
