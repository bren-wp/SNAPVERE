using System.ComponentModel;
using System.Runtime.InteropServices;
using Snapvere.Domain.Capture;

namespace Snapvere.Capture;

/// <summary>
/// Prefers the modern Windows capture backend but preserves a deterministic
/// compatibility path when Windows.Graphics.Capture cannot acquire a frame.
/// Caller cancellation is never converted into fallback work.
/// </summary>
public sealed class ResilientScreenCaptureService : IScreenCaptureService
{
    private readonly IScreenCaptureService _preferred;
    private readonly IScreenCaptureService _fallback;

    public ResilientScreenCaptureService(
        IScreenCaptureService preferred,
        IScreenCaptureService fallback)
    {
        _preferred = preferred ?? throw new ArgumentNullException(nameof(preferred));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
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
            return await _preferred
                .CaptureDisplayAsync(display, includeCursor, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (ShouldFallback(exception))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _fallback
                .CaptureDisplayAsync(display, includeCursor, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    internal static bool ShouldFallback(Exception exception)
        => exception is PlatformNotSupportedException
            or TimeoutException
            or COMException
            or Win32Exception
            or InvalidOperationException;
}
