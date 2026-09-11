using System.IO.Compression;

namespace Snapvere.Packaging;

public static class EmbeddedPayload
{
    private const int MaximumEntryCount = 20_000;
    private const long MaximumExpandedBytes = 4L * 1024 * 1024 * 1024;
    private const int CopyBufferSize = 128 * 1024;

    public static void ExtractZipSafely(
        Stream zipStream,
        string destinationRoot,
        Action<int, int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(zipStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);

        var root = Path.GetFullPath(destinationRoot);
        Directory.CreateDirectory(root);
        var rootWithSeparator = EnsureTrailingSeparator(root);

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        ValidateArchiveShape(archive);

        var extractedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

            var targetPath = GetValidatedTargetPath(root, rootWithSeparator, entry.FullName);

            if (IsDirectoryEntry(entry.FullName))
            {
                Directory.CreateDirectory(targetPath);
                continue;
            }

            var relativePath = Path.GetRelativePath(root, targetPath);
            if (!extractedFiles.Add(relativePath))
            {
                throw new InvalidDataException("The SNAPVERE package contains duplicate file destinations.");
            }

            var targetDirectory = Path.GetDirectoryName(targetPath)
                ?? throw new InvalidDataException("A SNAPVERE package entry has no parent directory.");
            Directory.CreateDirectory(targetDirectory);

            // Keep the staging component bounded independently of the payload
            // filename. A valid long NTFS filename must not become invalid just
            // because extraction appends a GUID and suffix to that basename.
            var temporaryPath = Path.Combine(
                targetDirectory,
                $".snapvere-{Guid.NewGuid():N}.tmp");
            try
            {
                using (var source = entry.Open())
                using (var destination = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: CopyBufferSize,
                    FileOptions.SequentialScan))
                {
                    source.CopyTo(destination, CopyBufferSize);
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

    /// <summary>
    /// Verifies that an extracted payload is an exact byte-for-byte projection
    /// of the embedded ZIP, apart from explicitly allowed launcher metadata.
    /// This is intended for user-writable caches that must not be trusted solely
    /// because a ready marker and executable are present.
    /// </summary>
    public static bool IsExtractedPayloadIntact(
        Stream zipStream,
        string destinationRoot,
        IReadOnlyCollection<string>? allowedExtraRelativePaths = null)
    {
        ArgumentNullException.ThrowIfNull(zipStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);

        var root = Path.GetFullPath(destinationRoot);
        if (!Directory.Exists(root))
        {
            return false;
        }

        var rootWithSeparator = EnsureTrailingSeparator(root);
        var expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allowedExtras = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (allowedExtraRelativePaths is not null)
        {
            foreach (var allowed in allowedExtraRelativePaths)
            {
                if (string.IsNullOrWhiteSpace(allowed) || Path.IsPathRooted(allowed))
                {
                    throw new ArgumentException(
                        "Allowed extra paths must be non-empty relative paths.",
                        nameof(allowedExtraRelativePaths));
                }

                var normalizedAllowed = Path.GetRelativePath(root, Path.GetFullPath(Path.Combine(root, allowed)));
                if (normalizedAllowed == "." || IsOutsideRoot(normalizedAllowed))
                {
                    throw new ArgumentException(
                        "Allowed extra paths must remain inside the payload root.",
                        nameof(allowedExtraRelativePaths));
                }

                allowedExtras.Add(normalizedAllowed);
            }
        }

        try
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
            ValidateArchiveShape(archive);

            long expandedBytes = 0;
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.FullName))
                {
                    continue;
                }

                expandedBytes = checked(expandedBytes + Math.Max(0, entry.Length));
                if (expandedBytes > MaximumExpandedBytes)
                {
                    throw new InvalidDataException("The SNAPVERE package expands beyond the allowed size.");
                }

                var targetPath = GetValidatedTargetPath(root, rootWithSeparator, entry.FullName);
                if (IsDirectoryEntry(entry.FullName))
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(root, targetPath);
                if (!expectedFiles.Add(relativePath))
                {
                    throw new InvalidDataException("The SNAPVERE package contains duplicate file destinations.");
                }

                if (!File.Exists(targetPath) || IsReparsePoint(targetPath))
                {
                    return false;
                }

                var fileInfo = new FileInfo(targetPath);
                if (fileInfo.Length != entry.Length)
                {
                    return false;
                }

                using var expected = entry.Open();
                using var actual = new FileStream(
                    targetPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: CopyBufferSize,
                    FileOptions.SequentialScan);

                if (!StreamsEqual(expected, actual))
                {
                    return false;
                }
            }

            return ContainsOnlyExpectedFiles(root, expectedFiles, allowedExtras);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            return false;
        }
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

    private static void ValidateArchiveShape(ZipArchive archive)
    {
        if (archive.Entries.Count > MaximumEntryCount)
        {
            throw new InvalidDataException("The SNAPVERE package contains too many files.");
        }
    }

    private static string GetValidatedTargetPath(string root, string rootWithSeparator, string entryName)
    {
        var normalizedEntryName = entryName.Replace('/', Path.DirectorySeparatorChar);
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

        return targetPath;
    }

    private static bool ContainsOnlyExpectedFiles(
        string root,
        IReadOnlySet<string> expectedFiles,
        IReadOnlySet<string> allowedExtras)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(root);

        while (pendingDirectories.Count > 0)
        {
            var current = pendingDirectories.Pop();
            foreach (var entryPath in Directory.EnumerateFileSystemEntries(current, "*", SearchOption.TopDirectoryOnly))
            {
                var attributes = File.GetAttributes(entryPath);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    return false;
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    pendingDirectories.Push(entryPath);
                    continue;
                }

                var relativePath = Path.GetRelativePath(root, entryPath);
                if (!expectedFiles.Contains(relativePath) && !allowedExtras.Contains(relativePath))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool StreamsEqual(Stream expected, Stream actual)
    {
        var expectedBuffer = new byte[CopyBufferSize];
        var actualBuffer = new byte[CopyBufferSize];

        while (true)
        {
            var expectedRead = expected.Read(expectedBuffer, 0, expectedBuffer.Length);
            var actualRead = actual.Read(actualBuffer, 0, actualBuffer.Length);
            if (expectedRead != actualRead)
            {
                return false;
            }

            if (expectedRead == 0)
            {
                return true;
            }

            if (!expectedBuffer.AsSpan(0, expectedRead).SequenceEqual(actualBuffer.AsSpan(0, actualRead)))
            {
                return false;
            }
        }
    }

    private static bool IsDirectoryEntry(string entryName)
        => entryName.EndsWith("/", StringComparison.Ordinal) ||
           entryName.EndsWith("\\", StringComparison.Ordinal);

    private static bool IsReparsePoint(string path)
        => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static bool IsOutsideRoot(string relativePath)
        => Path.IsPathRooted(relativePath) ||
           string.Equals(relativePath, "..", StringComparison.Ordinal) ||
           relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
           relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

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
