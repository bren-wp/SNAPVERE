using System.ComponentModel;
using System.Runtime.InteropServices;
using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Windows;

/// <summary>
/// Compatibility capture backend for environments where the primary
/// Windows.Graphics.Capture backend is unavailable. This is intentionally
/// isolated so the product can prefer the modern graphics-capture path.
/// </summary>
public sealed class GdiScreenCaptureService : IScreenCaptureService
{
    public ValueTask<CaptureFrame> CaptureDisplayAsync(
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

        var screenDc = NativeMethods.GetDC(nint.Zero);
        if (screenDc == nint.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetDC failed.");
        }

        nint memoryDc = nint.Zero;
        nint bitmap = nint.Zero;
        nint previousBitmap = nint.Zero;

        try
        {
            memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
            if (memoryDc == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateCompatibleDC failed.");
            }

            bitmap = NativeMethods.CreateCompatibleBitmap(screenDc, bounds.Width, bounds.Height);
            if (bitmap == nint.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateCompatibleBitmap failed.");
            }

            previousBitmap = NativeMethods.SelectObject(memoryDc, bitmap);
            if (previousBitmap == nint.Zero || previousBitmap == new nint(-1))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "SelectObject failed.");
            }

            var rasterOperation = NativeMethods.SourceCopy | NativeMethods.CaptureBlt;
            if (!NativeMethods.BitBlt(
                    memoryDc,
                    0,
                    0,
                    bounds.Width,
                    bounds.Height,
                    screenDc,
                    bounds.X,
                    bounds.Y,
                    rasterOperation))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "BitBlt failed while capturing the display.");
            }

            if (includeCursor)
            {
                DrawCursor(memoryDc, bounds);
            }

            var stride = checked(bounds.Width * 4);
            var pixels = new byte[checked(stride * bounds.Height)];
            var bitmapInfo = NativeMethods.BitmapInfo.CreateTopDownBgra32(bounds.Width, bounds.Height);

            var scanLines = NativeMethods.GetDIBits(
                memoryDc,
                bitmap,
                0,
                (uint)bounds.Height,
                pixels,
                ref bitmapInfo,
                NativeMethods.DibRgbColors);

            if (scanLines != bounds.Height)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "GetDIBits did not return the complete display frame.");
            }

            // GDI's unused high byte is not a reliable alpha channel. Normalize
            // screen captures to fully opaque BGRA8 so PNG export is deterministic.
            for (var alphaOffset = 3; alphaOffset < pixels.Length; alphaOffset += 4)
            {
                pixels[alphaOffset] = byte.MaxValue;
            }

            var frame = new CaptureFrame(
                new PixelSize(bounds.Width, bounds.Height),
                stride,
                pixels,
                DateTimeOffset.UtcNow,
                display.Id);

            frame.Validate();
            return ValueTask.FromResult(frame);
        }
        finally
        {
            if (memoryDc != nint.Zero && previousBitmap != nint.Zero && previousBitmap != new nint(-1))
            {
                _ = NativeMethods.SelectObject(memoryDc, previousBitmap);
            }

            if (bitmap != nint.Zero)
            {
                _ = NativeMethods.DeleteObject(bitmap);
            }

            if (memoryDc != nint.Zero)
            {
                _ = NativeMethods.DeleteDC(memoryDc);
            }

            _ = NativeMethods.ReleaseDC(nint.Zero, screenDc);
        }
    }

    private static void DrawCursor(nint destinationDc, PixelRect displayBounds)
    {
        var cursorInfo = new NativeMethods.CursorInfo
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.CursorInfo>()
        };

        if (!NativeMethods.GetCursorInfo(ref cursorInfo) ||
            (cursorInfo.Flags & NativeMethods.CursorShowing) == 0 ||
            cursorInfo.Cursor == nint.Zero)
        {
            return;
        }

        var point = cursorInfo.ScreenPosition;
        if (point.X < displayBounds.Left || point.X >= displayBounds.Right ||
            point.Y < displayBounds.Top || point.Y >= displayBounds.Bottom)
        {
            return;
        }

        var hotspotX = 0;
        var hotspotY = 0;
        NativeMethods.IconInfo iconInfo = default;

        try
        {
            if (NativeMethods.GetIconInfo(cursorInfo.Cursor, out iconInfo))
            {
                hotspotX = checked((int)iconInfo.HotspotX);
                hotspotY = checked((int)iconInfo.HotspotY);
            }

            _ = NativeMethods.DrawIconEx(
                destinationDc,
                checked(point.X - displayBounds.X - hotspotX),
                checked(point.Y - displayBounds.Y - hotspotY),
                cursorInfo.Cursor,
                0,
                0,
                0,
                nint.Zero,
                NativeMethods.DrawIconNormal);
        }
        finally
        {
            if (iconInfo.MaskBitmap != nint.Zero)
            {
                _ = NativeMethods.DeleteObject(iconInfo.MaskBitmap);
            }

            if (iconInfo.ColorBitmap != nint.Zero)
            {
                _ = NativeMethods.DeleteObject(iconInfo.ColorBitmap);
            }
        }
    }

    private static class NativeMethods
    {
        internal const uint SourceCopy = 0x00CC0020;
        internal const uint CaptureBlt = 0x40000000;
        internal const uint DibRgbColors = 0;
        internal const uint CursorShowing = 0x00000001;
        internal const uint DrawIconNormal = 0x0003;
        private const uint BiRgb = 0;

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            internal int X;
            internal int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CursorInfo
        {
            internal uint Size;
            internal uint Flags;
            internal nint Cursor;
            internal Point ScreenPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IconInfo
        {
            [MarshalAs(UnmanagedType.Bool)]
            internal bool IsIcon;
            internal uint HotspotX;
            internal uint HotspotY;
            internal nint MaskBitmap;
            internal nint ColorBitmap;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BitmapInfoHeader
        {
            internal uint Size;
            internal int Width;
            internal int Height;
            internal ushort Planes;
            internal ushort BitCount;
            internal uint Compression;
            internal uint SizeImage;
            internal int XPelsPerMeter;
            internal int YPelsPerMeter;
            internal uint ColorsUsed;
            internal uint ColorsImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BitmapInfo
        {
            internal BitmapInfoHeader Header;
            internal uint Colors;

            internal static BitmapInfo CreateTopDownBgra32(int width, int height)
                => new()
                {
                    Header = new BitmapInfoHeader
                    {
                        Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                        Width = width,
                        Height = checked(-height),
                        Planes = 1,
                        BitCount = 32,
                        Compression = BiRgb,
                        SizeImage = (uint)checked(width * height * 4)
                    }
                };
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern nint GetDC(nint window);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int ReleaseDC(nint window, nint deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern nint CreateCompatibleDC(nint deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC(nint deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern nint CreateCompatibleBitmap(nint deviceContext, int width, int height);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern nint SelectObject(nint deviceContext, nint graphicsObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(nint graphicsObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BitBlt(
            nint destination,
            int xDestination,
            int yDestination,
            int width,
            int height,
            nint source,
            int xSource,
            int ySource,
            uint rasterOperation);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern int GetDIBits(
            nint deviceContext,
            nint bitmap,
            uint startScan,
            uint scanLines,
            [Out] byte[] bits,
            ref BitmapInfo bitmapInfo,
            uint usage);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorInfo(ref CursorInfo cursorInfo);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetIconInfo(nint icon, out IconInfo iconInfo);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DrawIconEx(
            nint deviceContext,
            int x,
            int y,
            nint icon,
            int width,
            int height,
            uint stepIfAnimated,
            nint flickerFreeBrush,
            uint flags);
    }
}
