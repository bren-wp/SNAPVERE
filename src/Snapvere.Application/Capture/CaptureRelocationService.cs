using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Snapvere.Application.Capture;

/// <summary>
/// Moves a completed capture to a user-selected PNG destination without ever
/// exposing a partially copied destination file. The destination is populated
/// through a sibling temporary file and then atomically replaced. Source
/// deletion is tied to the exact open file handle that supplied the copied
/// bytes; if SNAPVERE cannot obtain that delete-capable snapshot handle, the
/// original is intentionally retained as a recovery copy.
/// </summary>
public sealed class CaptureRelocationService
{
    private const uint GenericRead = 0x80000000;
    private const uint DeleteAccess = 0x00010000;
    private const uint FileShareRead = 0x00000001;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x00000080;
    private const uint FileFlagOverlapped = 0x40000000;

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
            // Prefer a handle that combines read + DELETE access while sharing
            // reads only. While this handle remains open, another process
            // cannot replace, rename, truncate or delete the source path. That
            // lets deletion be applied to the exact file object that supplied
            // the copied bytes instead of performing a later path-based delete.
            await using var source = OpenSourceSnapshot(sourcePath, out var canDeleteByHandle);
            var copiedSourceIdentity = TryGetFileIdentity(source.SafeFileHandle);

            await CopyToTemporaryFileAsync(
                source,
                temporaryPath,
                cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, finalPath, overwrite: true);

            // If the stronger DELETE-capable handle could not be obtained (for
            // example because another reader denies delete sharing), leave the
            // original in place. The selected destination is already complete,
            // and a duplicate recovery copy is safer than deleting by pathname.
            if (canDeleteByHandle &&
                copiedSourceIdentity is { } copiedIdentity &&
                TryGetFileIdentity(finalPath) is { } destinationIdentity &&
                destinationIdentity != copiedIdentity)
            {
                TryMarkOpenedSourceForDeletion(source.SafeFileHandle);
            }
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }

        return capture with { FilePath = finalPath };
    }

    private static FileStream OpenSourceSnapshot(string sourcePath, out bool canDeleteByHandle)
    {
        var handle = NativeMethods.CreateFile(
            sourcePath,
            GenericRead | DeleteAccess,
            FileShareRead,
            nint.Zero,
            OpenExisting,
            FileAttributeNormal | FileFlagOverlapped,
            nint.Zero);

        if (!handle.IsInvalid)
        {
            try
            {
                canDeleteByHandle = true;
                return new FileStream(
                    handle,
                    FileAccess.Read,
                    bufferSize: 64 * 1024,
                    isAsync: true);
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        // Some filesystems, redirected folders or concurrently-open readers may
        // refuse DELETE access even though reading is valid. Fall back to a
        // read-only snapshot and deliberately preserve the source after copy.
        handle.Dispose();
        canDeleteByHandle = false;
        return new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);
    }

    private static async Task CopyToTemporaryFileAsync(
        FileStream source,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
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

            var first = TryGetFileIdentity(firstHandle);
            var second = TryGetFileIdentity(secondHandle);
            if (first is null || second is null)
            {
                return FileRelationship.Unknown;
            }

            return first.Value == second.Value
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

    private static FileIdentity? TryGetFileIdentity(string path)
    {
        try
        {
            using var handle = File.OpenHandle(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            return TryGetFileIdentity(handle);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static FileIdentity? TryGetFileIdentity(SafeFileHandle handle)
    {
        if (!NativeMethods.GetFileInformationByHandle(handle, out var information))
        {
            return null;
        }

        return new FileIdentity(
            information.VolumeSerialNumber,
            information.FileIndexHigh,
            information.FileIndexLow);
    }

    private static void TryMarkOpenedSourceForDeletion(SafeFileHandle sourceHandle)
    {
        var disposition = new FileDispositionInfo { DeleteFile = true };
        _ = NativeMethods.SetFileInformationByHandle(
            sourceHandle,
            FileInfoByHandleClass.FileDispositionInfo,
            ref disposition,
            (uint)Marshal.SizeOf<FileDispositionInfo>());
        // Destination is already complete. If Windows refuses the disposition
        // change, closing the handle simply preserves the source recovery copy.
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

    private readonly record struct FileIdentity(
        uint VolumeSerialNumber,
        uint FileIndexHigh,
        uint FileIndexLow);

    private enum FileRelationship
    {
        Unknown,
        Same,
        Different
    }

    private enum FileInfoByHandleClass
    {
        FileBasicInfo = 0,
        FileStandardInfo = 1,
        FileNameInfo = 2,
        FileRenameInfo = 3,
        FileDispositionInfo = 4
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

    [StructLayout(LayoutKind.Sequential)]
    private struct FileDispositionInfo
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool DeleteFile;
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            nint securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            nint templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetFileInformationByHandle(
            SafeFileHandle fileHandle,
            out ByHandleFileInformation fileInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetFileInformationByHandle(
            SafeFileHandle fileHandle,
            FileInfoByHandleClass fileInformationClass,
            ref FileDispositionInfo fileInformation,
            uint bufferSize);
    }
}
