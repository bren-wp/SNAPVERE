using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Snapvere.Domain.Capture;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32;
using WinRT;
using D3D = Windows.Win32.Graphics.Direct3D11;
using D3DCommon = Windows.Win32.Graphics.Direct3D;

namespace Snapvere.Capture.Windows;

public sealed partial class WindowsGraphicsCaptureService : IWindowCaptureService
{
    public async ValueTask<CaptureFrame> CaptureWindowAsync(
        WindowDescriptor window,
        bool includeCursor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(window);
        cancellationToken.ThrowIfCancellationRequested();

        if (window.NativeHandle == nint.Zero)
        {
            throw new ArgumentException("Window handle cannot be zero.", nameof(window));
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041) || !IsSupported())
        {
            throw new PlatformNotSupportedException(
                "Windows.Graphics.Capture window acquisition requires Windows 10 version 2004 (build 19041) or later.");
        }

        PInvoke.D3D11CreateDevice(
            pAdapter: null,
            D3DCommon.D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
            Software: default,
            D3D.D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
            pFeatureLevels: default,
            SDKVersion: D3D11SdkVersion,
            out var device,
            out _,
            out var context).ThrowOnFailure();

        try
        {
            var winrtDevice = CreateDirect3DDevice(device);
            try
            {
                var item = CreateItemForWindow(window.NativeHandle);
                using var framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                    winrtDevice,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    numberOfBuffers: 2,
                    item.Size);
                using var session = framePool.CreateCaptureSession(item);
                session.IsCursorCaptureEnabled = includeCursor;

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeout.Token);

                var frameSource = new TaskCompletionSource<Direct3D11CaptureFrame>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
                {
                    Direct3D11CaptureFrame? frame = null;
                    try
                    {
                        frame = sender.TryGetNextFrame();
                        if (frame is null)
                        {
                            return;
                        }

                        if (!frameSource.TrySetResult(frame))
                        {
                            frame.Dispose();
                        }
                    }
                    catch (Exception exception)
                    {
                        frame?.Dispose();
                        frameSource.TrySetException(exception);
                    }
                }

                framePool.FrameArrived += OnFrameArrived;
                try
                {
                    session.StartCapture();
                    using var frame = await frameSource.Task
                        .WaitAsync(linked.Token)
                        .ConfigureAwait(false);

                    var copied = CopyFrame(device, context, frame);
                    if (IsBlankCapture(copied.Pixels))
                    {
                        throw new InvalidOperationException(
                            "Windows.Graphics.Capture returned a blank window frame.");
                    }

                    var captureFrame = new CaptureFrame(
                        new PixelSize(copied.Width, copied.Height),
                        checked(copied.Width * 4),
                        copied.Pixels,
                        _timeProvider.GetUtcNow(),
                        $"wgc-window:0x{window.NativeHandle.ToInt64():X}");
                    captureFrame.Validate();
                    return captureFrame;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        "Windows.Graphics.Capture did not deliver a window frame within two seconds.");
                }
                finally
                {
                    framePool.FrameArrived -= OnFrameArrived;
                }
            }
            finally
            {
                (winrtDevice as IDisposable)?.Dispose();
            }
        }
        finally
        {
            (context as IDisposable)?.Dispose();
            (device as IDisposable)?.Dispose();
        }
    }

    private static unsafe GraphicsCaptureItem CreateItemForWindow(nint window)
    {
        using var factory = ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem");
        nint interopPointer = nint.Zero;
        nint itemPointer = nint.Zero;

        try
        {
            ThrowIfFailed(
                Marshal.QueryInterface(
                    factory.ThisPtr,
                    in GraphicsCaptureItemInteropGuid,
                    out interopPointer),
                "GraphicsCaptureItem.QueryInterface(IGraphicsCaptureItemInterop)");

            var interop = ComInterfaceMarshaller<IGraphicsCaptureItemInterop>
                .ConvertToManaged((void*)interopPointer)
                ?? throw new COMException("Could not project IGraphicsCaptureItemInterop.");
            interopPointer = nint.Zero;

            ThrowIfFailed(
                interop.CreateForWindow(window, in GraphicsCaptureItemGuid, out itemPointer),
                "GraphicsCaptureItem.CreateForWindow");

            var item = MarshalInspectable<GraphicsCaptureItem>.FromAbi(itemPointer);
            itemPointer = nint.Zero;
            return item;
        }
        finally
        {
            if (itemPointer != nint.Zero)
            {
                Marshal.Release(itemPointer);
            }

            if (interopPointer != nint.Zero)
            {
                ComInterfaceMarshaller<IGraphicsCaptureItemInterop>.Free((void*)interopPointer);
            }
        }
    }
}
