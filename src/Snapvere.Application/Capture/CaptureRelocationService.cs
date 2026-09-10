using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Snapvere.Application.Capture;

/// <summary>
/// Moves a completed capture to a user-selected PNG destination without ever
/// exposing a partially copied destination file. The destination is populated
/// through a sibling temporary file and then atomically replaced. If Windows
/// cannot prove that the source and destination are different files after the
/// destination is complete, the original is intentionally retained rather
/// than risking deletion through an alias, junction, hard link or mapped path.
/// </summary>
public sealed class CaptureRelocationService
{
    public async Task<CaptureSaveResult> RelocateAsync(
        CaptureSaveResult capture,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        cancellationToken.ThrowIfCancellationRequested();

        var sourcePath = Path.GetFullPath(capture.FilePath);
        var finalPath = Path.GetFullPath(destinationPath);

        if (!string.Equals(Path.GetExtension(finalPath), ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("SNAPVERE capture destinations must use the .png extension.", nameof(destinationPath));
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("The completed SNAPVERE capture could not be found.", sourcePath);
        }

        // Exact normalized paths are unambiguously the same destination. Do
        // not use an ordinal-ignore-case shortcut here: Windows supports
        // case-sensitive directories where names differing only by case can
        // identify different files. Case variants therefore go through the
        // actual file-identity check below.
        if (string.Equals(sourcePath, finalPath, StringComparison.Ordinal))
        {
            return capture with { FilePath = finalPath };
        }

        if (GetFileRelationship(sourcePath, finalPath) == FileRelationship.Same)
        {
            return capture with { FilePath = finalPath };
        }

        var destinationDirectory = Path.GetDirectoryName(finalPath)
            ?? throw new ArgumentException("The capture destination must include a parent directory.", nameof(destinationPath));
        Directory.CreateDirectory(destinationDirectory);

        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".snapvere-{Guid.NewGuid():N}.tmp");

        try
        {
            await CopyToTemporaryFileAsync(
                sourcePath,
                temporaryPath,
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, finalPath, overwrite: true);

            // Re-check identity after the atomic destination replacement. This
            // closes the dangerous case where two different path strings refer
            // to the same underlying file. Delete the source only when Windows
            // positively identifies source and destination as different files.
            if (GetFileRelationship(sourcePath, finalPath) == FileRelationship.Different)
            {
                TryDeleteCompletedSource(sourcePath);
            }
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }

        return capture with { FilePath = finalPath };
    }

    private static async Task CopyToTemporaryFileAsync(
        string sourcePath,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        // The completed capture is treated as an immutable snapshot while it
        // is copied. Other readers may inspect it, but writes, truncation and
        // deletion are denied until this stream is disposed.
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);
        await using var destination = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true);

        await source.CopyToAsync(destination, 64 * 1024, cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static FileRelationship GetFileRelationship(string firstPath, string secondPath)
    {
        if (!File.Exists(secondPath))
        {
            return FileRelationship.Different;
        }

        try
        {
            using var firstHandle = File.OpenHandle(
                firstPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var secondHandle = File.OpenHandle(
                secondPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            if (!NativeMethods.GetFileInformationByHandle(firstHandle, out var first) ||
                !NativeMethods.GetFileInformationByHandle(secondHandle, out var second))
            {
                return FileRelationship.Unknown;
            }

            return first.VolumeSerialNumber == second.VolumeSerialNumber &&
                   first.FileIndexHigh == second.FileIndexHigh &&
                   first.FileIndexLow == second.FileIndexLow
                ? FileRelationship.Same
                : FileRelationship.Different;
        }
        catch (IOException)
        {
            return FileRelationship.Unknown;
        }
        catch (UnauthorizedAccessException)
        {
            return FileRelationship.Unknown;
        }
    }

    private static void TryDeleteCompletedSource(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Destination is already complete. Keep the source as a recovery copy.
        }
        catch (UnauthorizedAccessException)
        {
            // Destination is already complete. Keep the source as a recovery copy.
        }
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
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private enum FileRelationship
    {
        Unknown,
        Same,
        Different
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public FileTime CreationTime;
        public FileTime LastAccessTime;
        public FileTime LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetFileInformationByHandle(
            SafeFileHandle fileHandle,
            out ByHandleFileInformation fileInformation);
    }
}
