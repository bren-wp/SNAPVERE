using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Snapvere.Packaging;

public static class EmbeddedPayload
{
    private const int MaximumEntryCount = 20_000;
    private const long MaximumExpandedBytes = 4L * 1024 * 1024 * 1024;
    private const int CopyBufferSize = 128 * 1024;
    private const int IntegrityManifestFormatVersion = 1;

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
    /// Verifies a user-writable extracted payload against a trusted SHA-256
    /// manifest embedded in the Portable host. The manifest is generated from
    /// the exact application payload at package-build time, so validation only
    /// needs one sequential read of each cached file and does not re-decompress
    /// the large embedded ZIP on every launch.
    /// </summary>
    public static bool IsExtractedPayloadIntact(
        Stream integrityManifestStream,
        string destinationRoot,
        IReadOnlyCollection<string>? allowedExtraRelativePaths = null)
    {
        ArgumentNullException.ThrowIfNull(integrityManifestStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRoot);

        var root = Path.GetFullPath(destinationRoot);
        if (!Directory.Exists(root))
        {
            return false;
        }

        var expectedFiles = ReadIntegrityManifest(integrityManifestStream, root);
        var allowedExtras = BuildAllowedExtraSet(root, allowedExtraRelativePaths);

        try
        {
            var matchedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pendingDirectories = new Stack<string>();
            pendingDirectories.Push(root);

            while (pendingDirectories.Count > 0)
            {
                var current = pendingDirectories.Pop();
                foreach (var entryPath in Directory.EnumerateFileSystemEntries(
                    current,
                    "*",
                    SearchOption.TopDirectoryOnly))
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
                    if (allowedExtras.Contains(relativePath))
                    {
                        continue;
                    }

                    if (!expectedFiles.TryGetValue(relativePath, out var expected) ||
                        !matchedFiles.Add(relativePath))
                    {
                        return false;
                    }

                    var fileInfo = new FileInfo(entryPath);
                    if (fileInfo.Length != expected.Length)
                    {
                        return false;
                    }

                    using var file = new FileStream(
                        entryPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        bufferSize: CopyBufferSize,
                        FileOptions.SequentialScan);
                    var actualHash = Convert.ToHexString(SHA256.HashData(file));
                    if (!string.Equals(actualHash, expected.Sha256, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return matchedFiles.Count == expectedFiles.Count;
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

    private static Dictionary<string, IntegrityManifestEntry> ReadIntegrityManifest(
        Stream manifestStream,
        string root)
    {
        IntegrityManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<IntegrityManifest>(
                manifestStream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The SNAPVERE payload integrity manifest is invalid JSON.", exception);
        }

        if (manifest is null || manifest.FormatVersion != IntegrityManifestFormatVersion)
        {
            throw new InvalidDataException("The SNAPVERE payload integrity manifest has an unsupported format.");
        }

        if (manifest.Files is null || manifest.Files.Length == 0 || manifest.Files.Length > MaximumEntryCount)
        {
            throw new InvalidDataException("The SNAPVERE payload integrity manifest has an invalid file count.");
        }

        var expectedFiles = new Dictionary<string, IntegrityManifestEntry>(
            manifest.Files.Length,
            StringComparer.OrdinalIgnoreCase);

        foreach (var entry in manifest.Files)
        {
            if (entry is null ||
                string.IsNullOrWhiteSpace(entry.Path) ||
                entry.Length < 0 ||
                !IsSha256Hex(entry.Sha256))
            {
                throw new InvalidDataException("The SNAPVERE payload integrity manifest contains an invalid file entry.");
            }

            var relativePath = NormalizeRelativePathInsideRoot(root, entry.Path);
            if (!expectedFiles.TryAdd(
                relativePath,
                new IntegrityManifestEntry(relativePath, entry.Length, entry.Sha256.ToUpperInvariant())))
            {
                throw new InvalidDataException("The SNAPVERE payload integrity manifest contains duplicate file destinations.");
            }
        }

        return expectedFiles;
    }

    private static HashSet<string> BuildAllowedExtraSet(
        string root,
        IReadOnlyCollection<string>? allowedExtraRelativePaths)
    {
        var allowedExtras = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (allowedExtraRelativePaths is null)
        {
            return allowedExtras;
        }

        foreach (var allowed in allowedExtraRelativePaths)
        {
            if (string.IsNullOrWhiteSpace(allowed))
            {
                throw new ArgumentException(
                    "Allowed extra paths must be non-empty relative paths.",
                    nameof(allowedExtraRelativePaths));
            }

            try
            {
                allowedExtras.Add(NormalizeRelativePathInsideRoot(root, allowed));
            }
            catch (InvalidDataException exception)
            {
                throw new ArgumentException(
                    "Allowed extra paths must remain inside the payload root.",
                    nameof(allowedExtraRelativePaths),
                    exception);
            }
        }

        return allowedExtras;
    }

    private static string NormalizeRelativePathInsideRoot(string root, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized))
        {
            throw new InvalidDataException("A SNAPVERE payload path must be relative.");
        }

        var rootWithSeparator = EnsureTrailingSeparator(root);
        var target = Path.GetFullPath(Path.Combine(root, normalized));
        if (!target.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("A SNAPVERE payload path escapes its destination root.");
        }

        var canonicalRelative = Path.GetRelativePath(root, target);
        if (canonicalRelative == "." || IsOutsideRoot(canonicalRelative))
        {
            throw new InvalidDataException("A SNAPVERE payload path is not a valid file destination.");
        }

        return canonicalRelative;
    }

    private static bool IsSha256Hex(string? value)
    {
        if (value is null || value.Length != 64)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!Uri.IsHexDigit(character))
            {
                return false;
            }
        }

        return true;
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

    private static bool IsDirectoryEntry(string entryName)
        => entryName.EndsWith("/", StringComparison.Ordinal) ||
           entryName.EndsWith("\\", StringComparison.Ordinal);

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

    private sealed class IntegrityManifest
    {
        public int FormatVersion { get; init; }

        public IntegrityManifestEntry?[]? Files { get; init; }
    }

    private sealed record IntegrityManifestEntry(string Path, long Length, string Sha256);
}
