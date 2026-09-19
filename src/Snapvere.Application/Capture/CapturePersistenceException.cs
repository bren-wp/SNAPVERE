using System.Security;

namespace Snapvere.Application.Capture;

public enum CapturePersistenceFailureKind
{
    AccessDenied,
    StorageFull,
    WriteFailed
}

/// <summary>
/// Stable classification for expected local persistence failures. Technical
/// exception details remain available through <see cref="Exception.InnerException"/>
/// for local diagnostics and are never intended as user-facing copy.
/// </summary>
public sealed class CapturePersistenceException : Exception
{
    public CapturePersistenceException(
        CapturePersistenceFailureKind kind,
        Exception innerException)
        : base("SNAPVERE could not persist the capture to local storage.", innerException)
    {
        Kind = kind;
    }

    public CapturePersistenceFailureKind Kind { get; }
}

public static class CapturePersistenceFailurePolicy
{
    private const int ErrorHandleDiskFull = 39;
    private const int ErrorDiskFull = 112;

    public static bool TryClassify(
        Exception exception,
        out CapturePersistenceFailureKind kind)
    {
        ArgumentNullException.ThrowIfNull(exception);

        switch (exception)
        {
            case UnauthorizedAccessException:
            case SecurityException:
                kind = CapturePersistenceFailureKind.AccessDenied;
                return true;

            case IOException ioException:
                var win32Code = ioException.HResult & 0xFFFF;
                kind = win32Code is ErrorDiskFull or ErrorHandleDiskFull
                    ? CapturePersistenceFailureKind.StorageFull
                    : CapturePersistenceFailureKind.WriteFailed;
                return true;

            default:
                kind = default;
                return false;
        }
    }

    public static CapturePersistenceException Wrap(Exception exception)
    {
        if (!TryClassify(exception, out var kind))
        {
            throw new ArgumentException(
                "The exception is not an expected capture-persistence failure.",
                nameof(exception));
        }

        return new CapturePersistenceException(kind, exception);
    }
}
