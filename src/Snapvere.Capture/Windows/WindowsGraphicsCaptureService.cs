using Microsoft.Graphics.Canvas;
using Snapvere.Domain.Capture;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using WinRT;

namespace Snapvere.Capture.Windows;

/// <summary>
/// Primary Windows capture backend based on Windows.Graphics.Capture.
/// The captured Direct3D surface is read back through Win2D so this layer
/// can keep returning the product's stable BGRA8 <see cref="CaptureFrame"/>
/// contract without leaking Direct3D objects into the application layer.
/// </summary>
public sealed class WindowsGraphicsCaptureService : IScreenCaptureService
{
    private const string GraphicsCaptureSessionTypeName =
        "Windows.Graphics.Capture.GraphicsCaptureSession";
    private const string CursorCapturePropertyName = "IsCursorCaptureEnabled";
    private const uint MonitorDefaultToNull = 0;

    private static readonly Guid GraphicsCaptureItemGuid =
        new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    private static readonly TimeSpan FirstFrameTimeout = TimeSpan.FromSeconds(5);

    // Do not create the Direct3D device during application startup. Keeping the
    // graphics device lazy isolates WinUI startup from GPU/driver initialization
    // and lets the compatibility backend remain usable if device creation fails.
    private readonly Lazy<CanvasDevice> _canvasDevice = new(
        CanvasDevice.GetSharedDevice,
        LazyThreadSafetyMode.ExecutionAndPublication);

    public bool IsSupported
        => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362) &&
           GraphicsCaptureSession.IsSupported();

    public async ValueTask<CaptureFrame> CaptureDisplayAsync(
        DisplayDescriptor display,
        bool includeCursor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(display);
        cancellationToken.ThrowIfCancellationRequested();

        var bounds = display.Bounds.Normalize();
        if (bounds.IsEmpty)
        {
            throw new ArgumentException("Display bounds must be non-empty.", nameof(display));
        }

        if (!IsSupported)
        {
            throw new NotSupportedException(
                "Windows.Graphics.Capture monitor capture is unavailable on this Windows installation.");
        }

        var monitor = ResolveMonitor(bounds);
        if (monitor == nint.Zero)
        {
            throw new InvalidOperationException(
                $"Windows could not resolve a monitor handle for display '{display.Id}'.");
        }

        var captureItem = CreateItemForMonitor(monitor);
        var initialSize = captureItem.Size;
        if (initialSize.Width <= 0 || initialSize.Height <= 0)
        {
            throw new InvalidOperationException(
                $"Windows.Graphics.Capture returned an empty target for display '{display.Id}'.");
        }

        var canvasDevice = _canvasDevice.Value;
        using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            canvasDevice,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            initialSize);
        using var session = framePool.CreateCaptureSession(captureItem);

        ConfigureCursorCapture(session, includeCursor);

        var completion = new TaskCompletionSource<CaptureFrame>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var completionState = 0;

        TypedEventHandler<Direct3D11CaptureFramePool, object>? frameArrived = null;
        frameArrived = (sender, _) =>
        {
            if (Volatile.Read(ref completionState) != 0)
            {
                return;
            }

            try
            {
                using var frame = sender.TryGetNextFrame();
                if (frame is null)
                {
                    return;
                }

                var captureFrame = ReadFrame(canvasDevice, frame, display.Id);
                if (Interlocked.CompareExchange(ref completionState, 1, 0) == 0)
                {
                    completion.TrySetResult(captureFrame);
                }
            }
            catch (Exception exception)
            {
                if (Interlocked.CompareExchange(ref completionState, 1, 0) == 0)
                {
                    completion.TrySetException(exception);
                }
            }
        };

        framePool.FrameArrived += frameArrived;

        try
        {
            session.StartCapture();

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(FirstFrameTimeout);

            try
            {
                return await completion.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Windows.Graphics.Capture did not deliver a frame for display '{display.Id}' within {FirstFrameTimeout.TotalSeconds:0} seconds.");
            }
        }
        finally
        {
            framePool.FrameArrived -= frameArrived;
            Interlocked.Exchange(ref completionState, 1);
        }
    }

    private static CaptureFrame ReadFrame(
        CanvasDevice canvasDevice,
        Direct3D11CaptureFrame frame,
        string sourceId)
    {
        var contentSize = frame.ContentSize;
        if (contentSize.Width <= 0 || contentSize.Height <= 0)
        {
            throw new InvalidOperationException("Windows.Graphics.Capture delivered an empty frame.");
        }

        using var bitmap = CanvasBitmap.CreateFromDirect3D11Surface(
            canvasDevice,
            frame.Surface);

        var bitmapSize = bitmap.SizeInPixels;
        var bitmapWidth = checked((int)bitmapSize.Width);
        var bitmapHeight = checked((int)bitmapSize.Height);
        var width = Math.Min(contentSize.Width, bitmapWidth);
        var height = Math.Min(contentSize.Height, bitmapHeight);

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("The captured Direct3D surface has no readable pixels.");
        }

        var pixels = bitmap.GetPixelBytes(0, 0, width, height);
        var stride = checked(width * 4);
        var requiredLength = checked(stride * height);

        if (pixels.Length != requiredLength)
        {
            throw new InvalidOperationException(
                $"Unexpected BGRA8 frame length. Expected {requiredLength} bytes, received {pixels.Length}.");
        }

        // Monitor capture is opaque. Normalizing alpha avoids downstream PNG
        // differences caused by implementation-specific surface alpha values.
        for (var alphaOffset = 3; alphaOffset < pixels.Length; alphaOffset += 4)
        {
            pixels[alphaOffset] = byte.MaxValue;
        }

        var result = new CaptureFrame(
            new PixelSize(width, height),
            stride,
            pixels,
            DateTimeOffset.UtcNow,
            sourceId);
        result.Validate();
        return result;
    }

    private static void ConfigureCursorCapture(GraphicsCaptureSession session, bool includeCursor)
    {
        if (ApiInformation.IsPropertyPresent(
                GraphicsCaptureSessionTypeName,
                CursorCapturePropertyName))
        {
            session.IsCursorCaptureEnabled = includeCursor;
            return;
        }

        // Windows 10 1903 exposes CreateForMonitor but does not expose the
        // cursor toggle added in 2004. Falling back is the only way to honor a
        // request that explicitly excludes the cursor on that OS release.
        if (!includeCursor)
        {
            throw new NotSupportedException(
                "This Windows version cannot disable cursor capture in Windows.Graphics.Capture.");
        }
    }

    private static GraphicsCaptureItem CreateItemForMonitor(nint monitor)
    {
        var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
        var itemPointer = interop.CreateForMonitor(monitor, GraphicsCaptureItemGuid);

        if (itemPointer == nint.Zero)
        {
            throw new InvalidOperationException(
                "IGraphicsCaptureItemInterop.CreateForMonitor returned a null capture item.");
        }

        try
        {
            return GraphicsCaptureItem.FromAbi(itemPointer);
        }
        finally
        {
            _ = Marshal.Release(itemPointer);
        }
    }

    private static nint ResolveMonitor(PixelRect bounds)
    {
        var nativeBounds = new NativeMethods.Rect
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Right = bounds.Right,
            Bottom = bounds.Bottom
        };

        return NativeMethods.MonitorFromRect(ref nativeBounds, MonitorDefaultToNull);
    }

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        nint CreateForWindow(nint window, in Guid iid);

        nint CreateForMonitor(nint monitor, in Guid iid);
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [DllImport("user32.dll")]
        internal static extern nint MonitorFromRect(ref Rect rect, uint flags);
    }
}
