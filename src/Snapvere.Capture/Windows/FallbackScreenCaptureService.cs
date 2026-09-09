using System.Runtime.InteropServices;
using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Windows;

/// <summary>
/// Prefers the modern Windows.Graphics.Capture implementation and uses the
/// compatibility backend only when the modern path is unavailable or fails
/// with a recoverable graphics/platform error.
/// </summary>
public sealed class FallbackScreenCaptureService : IScreenCaptureService
{
    private readonly IScreenCaptureService _primary;
    private readonly IScreenCaptureService _fallback;

    public FallbackScreenCaptureService(
        IScreenCaptureService primary,
        IScreenCaptureService fallback)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));

        if (ReferenceEquals(_primary, _fallback))
        {
            throw new ArgumentException(
                "Primary and fallback capture services must be different instances.",
                nameof(fallback));
        }
    }

    public async ValueTask<CaptureFrame> CaptureDisplayAsync(
        DisplayDescriptor display,
        bool includeCursor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(display);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await _primary
                .CaptureDisplayAsync(display, includeCursor, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (ShouldUseFallback(exception, cancellationToken))
        {
            return await _fallback
                .CaptureDisplayAsync(display, includeCursor, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static bool ShouldUseFallback(
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested ||
            exception is OperationCanceledException)
        {
            return false;
        }

        return exception is NotSupportedException or
            PlatformNotSupportedException or
            COMException or
            TimeoutException or
            InvalidOperationException;
    }
}
