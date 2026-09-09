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
using DxgiCommon = Windows.Win32.Graphics.Dxgi.Common;

namespace Snapvere.Capture.Windows;

/// <summary>
/// Primary Windows.Graphics.Capture monitor backend. It creates a free-threaded
/// frame pool, copies the captured D3D11 texture into a CPU-readable staging
/// texture, and returns a packed BGRA8 frame. Platform/native acquisition
/// failures are handled by ResilientScreenCaptureService, which falls back to
/// the GDI compatibility backend.
/// </summary>
public sealed partial class WindowsGraphicsCaptureService : IScreenCaptureService
{
    private const uint D3D11SdkVersion = 7;
    private const uint MonitorDefaultToNull = 0;

    private static readonly Guid GraphicsCaptureItemGuid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    private static readonly Guid GraphicsCaptureItemInteropGuid = new("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");
    private static readonly Guid Direct3DDxgiInterfaceAccessGuid = new("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");
    private static readonly Guid DxgiDeviceGuid = new("54EC77FA-1377-44E6-8C32-88FD5F44C84C");
    private static readonly Guid D3D11Texture2DGuid = new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

    private readonly TimeProvider _timeProvider;

    public WindowsGraphicsCaptureService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public static bool IsSupported()
    {
        try
        {
            return GraphicsCaptureSession.IsSupported();
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask<CaptureFrame> CaptureDisplayAsync(
        DisplayDescriptor display,
        bool includeCursor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(display);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsSupported())
        {
            throw new PlatformNotSupportedException(
                "Windows.Graphics.Capture is not supported on this Windows installation.");
        }

        var bounds = display.Bounds.Normalize();
        if (bounds.IsEmpty)
        {
            throw new ArgumentException("Display bounds must contain at least one pixel.", nameof(display));
        }

        var monitor = ResolveMonitor(bounds);
        if (monitor == nint.Zero)
        {
            throw new InvalidOperationException(
                "SNAPVERE could not resolve the native monitor handle for the selected display.");
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
                var item = CreateItemForMonitor(monitor);
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
                            "Windows.Graphics.Capture returned a blank monitor frame.");
                    }

                    if (copied.Width != bounds.Width || copied.Height != bounds.Height)
                    {
                        throw new InvalidOperationException(
                            "Windows.Graphics.Capture returned a frame whose dimensions no longer match the selected monitor.");
                    }

                    var captureFrame = new CaptureFrame(
                        new PixelSize(copied.Width, copied.Height),
                        checked(copied.Width * 4),
                        copied.Pixels,
                        _timeProvider.GetUtcNow(),
                        $"wgc:{display.Id}");
                    captureFrame.Validate();
                    return captureFrame;
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        "Windows.Graphics.Capture did not deliver a monitor frame within two seconds.");
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

    private static nint ResolveMonitor(PixelRect bounds)
    {
        var center = new NativePoint
        {
            X = checked(bounds.X + (bounds.Width / 2)),
            Y = checked(bounds.Y + (bounds.Height / 2))
        };

        return MonitorFromPoint(center, MonitorDefaultToNull);
    }

    private static unsafe IDirect3DDevice CreateDirect3DDevice(D3D.ID3D11Device device)
    {
        var d3dDevicePointer = ComInterfaceMarshaller<D3D.ID3D11Device>.ConvertToUnmanaged(device);
        nint dxgiDevicePointer = nint.Zero;
        nint graphicsDevicePointer = nint.Zero;

        try
        {
            Marshal.QueryInterface(
                    (nint)d3dDevicePointer,
                    in DxgiDeviceGuid,
                    out dxgiDevicePointer)
                .ThrowIfFailed("ID3D11Device.QueryInterface(IDXGIDevice)");

            CreateDirect3D11DeviceFromDXGIDevice(
                    dxgiDevicePointer,
                    out graphicsDevicePointer)
                .ThrowIfFailed("CreateDirect3D11DeviceFromDXGIDevice");

            var managed = MarshalInspectable<IDirect3DDevice>.FromAbi(graphicsDevicePointer);
            graphicsDevicePointer = nint.Zero;
            return managed;
        }
        finally
        {
            if (graphicsDevicePointer != nint.Zero)
            {
                Marshal.Release(graphicsDevicePointer);
            }

            if (dxgiDevicePointer != nint.Zero)
            {
                Marshal.Release(dxgiDevicePointer);
            }

            if (d3dDevicePointer is not null)
            {
                ComInterfaceMarshaller<D3D.ID3D11Device>.Free(d3dDevicePointer);
            }
        }
    }

    private static unsafe GraphicsCaptureItem CreateItemForMonitor(nint monitor)
    {
        using var factory = ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem");
        nint interopPointer = nint.Zero;
        nint itemPointer = nint.Zero;

        try
        {
            Marshal.QueryInterface(
                    factory.ThisPtr,
                    in GraphicsCaptureItemInteropGuid,
                    out interopPointer)
                .ThrowIfFailed("GraphicsCaptureItem.QueryInterface(IGraphicsCaptureItemInterop)");

            var interop = ComInterfaceMarshaller<IGraphicsCaptureItemInterop>
                .ConvertToManaged((void*)interopPointer)
                ?? throw new COMException("Could not project IGraphicsCaptureItemInterop.");
            interopPointer = nint.Zero;

            interop.CreateForMonitor(monitor, in GraphicsCaptureItemGuid, out itemPointer)
                .ThrowIfFailed("GraphicsCaptureItem.CreateForMonitor");

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

    private static unsafe (byte[] Pixels, int Width, int Height) CopyFrame(
        D3D.ID3D11Device device,
        D3D.ID3D11DeviceContext context,
        Direct3D11CaptureFrame frame)
    {
        var capturedTexture = GetTexture(frame.Surface);
        try
        {
            var size = frame.ContentSize;
            var width = size.Width;
            var height = size.Height;
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException("Windows.Graphics.Capture returned an empty frame.");
            }

            var description = new D3D.D3D11_TEXTURE2D_DESC
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = DxgiCommon.DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleDesc = new DxgiCommon.DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                Usage = D3D.D3D11_USAGE.D3D11_USAGE_STAGING,
                BindFlags = 0,
                CPUAccessFlags = D3D.D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,
                MiscFlags = 0
            };

            device.CreateTexture2D(in description, pInitialData: null, out var stagingTexture);
            try
            {
                context.CopyResource(stagingTexture, capturedTexture);
                context.Map(stagingTexture, 0, D3D.D3D11_MAP.D3D11_MAP_READ, 0, out var mapped);
                try
                {
                    var rowBytes = checked(width * 4);
                    var pixels = new byte[checked(rowBytes * height)];
                    fixed (byte* destination = pixels)
                    {
                        for (var row = 0; row < height; row++)
                        {
                            Buffer.MemoryCopy(
                                (byte*)mapped.pData + (row * mapped.RowPitch),
                                destination + (row * rowBytes),
                                rowBytes,
                                rowBytes);
                        }
                    }

                    return (pixels, width, height);
                }
                finally
                {
                    context.Unmap(stagingTexture, 0);
                }
            }
            finally
            {
                (stagingTexture as IDisposable)?.Dispose();
            }
        }
        finally
        {
            (capturedTexture as IDisposable)?.Dispose();
        }
    }

    private static unsafe D3D.ID3D11Texture2D GetTexture(IDirect3DSurface surface)
    {
        var surfacePointer = ((IWinRTObject)surface).NativeObject.ThisPtr;
        nint accessPointer = nint.Zero;
        nint texturePointer = nint.Zero;

        try
        {
            Marshal.QueryInterface(
                    surfacePointer,
                    in Direct3DDxgiInterfaceAccessGuid,
                    out accessPointer)
                .ThrowIfFailed("IDirect3DSurface.QueryInterface(IDirect3DDxgiInterfaceAccess)");

            var access = ComInterfaceMarshaller<IDirect3DDxgiInterfaceAccess>
                .ConvertToManaged((void*)accessPointer)
                ?? throw new COMException("Could not project IDirect3DDxgiInterfaceAccess.");
            accessPointer = nint.Zero;

            access.GetInterface(in D3D11Texture2DGuid, out texturePointer)
                .ThrowIfFailed("IDirect3DDxgiInterfaceAccess.GetInterface");

            var texture = ComInterfaceMarshaller<D3D.ID3D11Texture2D>
                .ConvertToManaged((void*)texturePointer)
                ?? throw new COMException("Could not project the captured ID3D11Texture2D.");
            texturePointer = nint.Zero;
            return texture;
        }
        finally
        {
            if (texturePointer != nint.Zero)
            {
                Marshal.Release(texturePointer);
            }

            if (accessPointer != nint.Zero)
            {
                ComInterfaceMarshaller<IDirect3DDxgiInterfaceAccess>.Free((void*)accessPointer);
            }
        }
    }

    internal static bool IsBlankCapture(byte[] pixels)
    {
        var chunks = MemoryMarshal.Cast<byte, long>(pixels.AsSpan());
        foreach (var chunk in chunks)
        {
            if (chunk != 0)
            {
                return false;
            }
        }

        for (var index = chunks.Length * sizeof(long); index < pixels.Length; index++)
        {
            if (pixels[index] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static void ThrowIfFailed(this int hresult, string operation)
    {
        if (hresult < 0)
        {
            throw new COMException($"{operation} failed with HRESULT 0x{hresult:X8}.", hresult);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        internal int X;
        internal int Y;
    }

    [GeneratedComInterface]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    private partial interface IGraphicsCaptureItemInterop
    {
        [PreserveSig]
        int CreateForWindow(nint window, in Guid iid, out nint result);

        [PreserveSig]
        int CreateForMonitor(nint monitor, in Guid iid, out nint result);
    }

    [GeneratedComInterface]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    private partial interface IDirect3DDxgiInterfaceAccess
    {
        [PreserveSig]
        int GetInterface(in Guid iid, out nint result);
    }

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromPoint(NativePoint point, uint flags);

    [LibraryImport("d3d11.dll")]
    private static partial int CreateDirect3D11DeviceFromDXGIDevice(
        nint dxgiDevice,
        out nint graphicsDevice);
}
