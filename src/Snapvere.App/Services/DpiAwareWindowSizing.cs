using Microsoft.UI.Xaml;
using Snapvere.Shared;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace Snapvere.App.Services;

/// <summary>
/// Converts WinUI design sizes expressed in DIPs to the physical-pixel sizes
/// required by AppWindow.Resize and, when possible, clamps the result to the
/// monitor work area so high-DPI secondary surfaces remain reachable.
/// </summary>
internal static class DpiAwareWindowSizing
{
    private const double BaselineDpi = 96.0;
    private const uint MonitorDefaultToNearest = 2;

    internal static SizeInt32 ScaleSize(Window window, int widthInDips, int heightInDips)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widthInDips);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(heightInDips);

        var scale = GetScale(window);
        return Scale(widthInDips, heightInDips, scale);
    }

    internal static SizeInt32 ScaleSizeToWorkArea(
        Window window,
        int widthInDips,
        int heightInDips,
        int outerMarginInDips = 12)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widthInDips);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(heightInDips);
        ArgumentOutOfRangeException.ThrowIfNegative(outerMarginInDips);

        var scale = GetScale(window);
        var desired = Scale(widthInDips, heightInDips, scale);

        try
        {
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (handle == nint.Zero)
            {
                return desired;
            }

            var monitor = NativeMethods.MonitorFromWindow(handle, MonitorDefaultToNearest);
            if (monitor == nint.Zero)
            {
                return desired;
            }

            var info = new NativeMethods.MonitorInfo
            {
                Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>()
            };
            if (!NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                return desired;
            }

            var workAreaWidth = Math.Max(1, info.WorkArea.Right - info.WorkArea.Left);
            var workAreaHeight = Math.Max(1, info.WorkArea.Bottom - info.WorkArea.Top);
            var margin = checked((int)Math.Ceiling(outerMarginInDips * scale));
            var fitted = ResponsiveWindowSizePolicy.FitWithinWorkArea(
                desired.Width,
                desired.Height,
                workAreaWidth,
                workAreaHeight,
                margin);

            return new SizeInt32(fitted.Width, fitted.Height);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            COMException or
            OverflowException)
        {
            // If HWND/monitor metadata is temporarily unavailable, keep the
            // established DPI-aware fallback instead of failing UI creation.
            return desired;
        }
    }

    private static SizeInt32 Scale(int widthInDips, int heightInDips, double scale)
        => new(
            checked((int)Math.Ceiling(widthInDips * scale)),
            checked((int)Math.Ceiling(heightInDips * scale)));

    private static double GetScale(Window window)
    {
        try
        {
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (handle == nint.Zero)
            {
                return 1.0;
            }

            var dpi = NativeMethods.GetDpiForWindow(handle);
            return dpi == 0 ? 1.0 : dpi / BaselineDpi;
        }
        catch (Exception exception) when (exception is InvalidOperationException or COMException)
        {
            // A not-yet-materialized HWND should not make a secondary surface
            // unusable. 96 DPI is the safe fallback and matches CI runners.
            return 1.0;
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern uint GetDpiForWindow(nint window);

        [DllImport("user32.dll")]
        internal static extern nint MonitorFromWindow(nint window, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MonitorInfo
        {
            internal uint Size;
            internal Rect Monitor;
            internal Rect WorkArea;
            internal uint Flags;
        }
    }
}
