using System.IO.Compression;

namespace Snapvere.Packaging;

public static class EmbeddedPayload
{
    private const int MaximumEntryCount = 20_000;
    private const long MaximumExpandedBytes = 4L * 1024 * 1024 * 1024;

    public static void ExtractZipSafely(
        Stream zipStream,
        string destinationRoot,
        Action<int, int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(zipStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);

        var root = Path.GetFullPath(destinationRoot);
        Directory.CreateDirectory(root);
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > MaximumEntryCount)
        {
            throw new InvalidDataException("The SNAPVERE package contains too many files.");
        }

        long expandedBytes = 0;
        for (var index = 0; index < archive.Entries.Count; index++)
        {
            var entry = archive.Entries[index];
            progress?.Invoke(index, archive.Entries.Count);

            if (string.IsNullOrWhiteSpace(entry.FullName))
            {
                continue;
            }

            expandedBytes = checked(expandedBytes + Math.Max(0, entry.Length));
            if (expandedBytes > MaximumExpandedBytes)
            {
                throw new InvalidDataException("The SNAPVERE package expands beyond the allowed size.");
            }

            var normalizedEntryName = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(normalizedEntryName))
            {
                throw new InvalidDataException("The SNAPVERE package contains an absolute path.");
            }

            var targetPath = Path.GetFullPath(Path.Combine(root, normalizedEntryName));
            if (!targetPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(targetPath, root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The SNAPVERE package attempted to write outside its destination.");
            }

            if (entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
                entry.FullName.EndsWith("\\", StringComparison.Ordinal))
            {
                Directory.CreateDirectory(targetPath);
                continue;
            }

            var targetDirectory = Path.GetDirectoryName(targetPath)
                ?? throw new InvalidDataException("A SNAPVERE package entry has no parent directory.");
            Directory.CreateDirectory(targetDirectory);

            var temporaryPath = $"{targetPath}.snapvere-{Guid.NewGuid():N}.tmp";
            try
            {
                using (var source = entry.Open())
                using (var destination = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 128 * 1024,
                    FileOptions.SequentialScan))
                {
                    source.CopyTo(destination, 128 * 1024);
                    destination.Flush(flushToDisk: true);
                }

                File.Move(temporaryPath, targetPath, overwrite: true);
            }
            finally
            {
                TryDeleteFile(temporaryPath);
            }
        }

        progress?.Invoke(archive.Entries.Count, archive.Entries.Count);
    }

    public static void DeleteDirectoryBestEffort(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteFile(string path)
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
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
