using Snapvere.Domain.Capture;
using System.Runtime.InteropServices;
using System.Text;

namespace Snapvere.Capture.Windows;

/// <summary>
/// Enumerates capturable desktop top-level windows in native Z-order.
/// The discovery layer intentionally excludes SNAPVERE's own windows,
/// invisible/cloaked windows and zero-area targets.
/// </summary>
public sealed class Win32WindowDiscovery : IWindowDiscovery
{
    private const int DwmExtendedFrameBounds = 9;
    private const int DwmCloaked = 14;
    private const uint GetAncestorRoot = 2;

    private readonly uint _currentProcessId = unchecked((uint)Environment.ProcessId);

    public IReadOnlyList<WindowDescriptor> GetWindows()
    {
        var windows = new List<WindowDescriptor>();
        var callback = new NativeMethods.EnumWindowsProc((window, _) =>
        {
            var descriptor = TryDescribe(window);
            if (descriptor is not null)
            {
                windows.Add(descriptor);
            }

            return true;
        });

        if (!NativeMethods.EnumWindows(callback, nint.Zero))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                throw new InvalidOperationException($"EnumWindows failed with Win32 error {error}.");
            }
        }

        GC.KeepAlive(callback);
        return windows;
    }

    public WindowDescriptor? TryGetWindowAtPoint(PixelPoint point)
    {
        var window = NativeMethods.WindowFromPoint(new NativeMethods.Point(point.X, point.Y));
        if (window == nint.Zero)
        {
            return null;
        }

        var root = NativeMethods.GetAncestor(window, GetAncestorRoot);
        return TryDescribe(root == nint.Zero ? window : root);
    }

    private WindowDescriptor? TryDescribe(nint window)
    {
        if (window == nint.Zero || !NativeMethods.IsWindowVisible(window))
        {
            return null;
        }

        _ = NativeMethods.GetWindowThreadProcessId(window, out var processId);
        if (processId == 0 || processId == _currentProcessId)
        {
            return null;
        }

        if (IsCloaked(window))
        {
            return null;
        }

        var title = ReadWindowText(window);
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        if (!TryGetBounds(window, out var bounds) || bounds.IsEmpty)
        {
            return null;
        }

        return new WindowDescriptor(
            window,
            title.Trim(),
            bounds,
            processId,
            ReadClassName(window));
    }

    private static bool IsCloaked(nint window)
    {
        var cloaked = 0;
        var result = NativeMethods.DwmGetWindowAttribute(
            window,
            DwmCloaked,
            out cloaked,
            Marshal.SizeOf<int>());
        return result >= 0 && cloaked != 0;
    }

    private static bool TryGetBounds(nint window, out PixelRect bounds)
    {
        NativeMethods.Rect rectangle;
        var result = NativeMethods.DwmGetWindowAttribute(
            window,
            DwmExtendedFrameBounds,
            out rectangle,
            Marshal.SizeOf<NativeMethods.Rect>());

        if (result < 0 && !NativeMethods.GetWindowRect(window, out rectangle))
        {
            bounds = default;
            return false;
        }

        var width = rectangle.Right - rectangle.Left;
        var height = rectangle.Bottom - rectangle.Top;
        if (width <= 0 || height <= 0)
        {
            bounds = default;
            return false;
        }

        bounds = new PixelRect(rectangle.Left, rectangle.Top, width, height);
        return true;
    }

    private static string ReadWindowText(nint window)
    {
        var length = NativeMethods.GetWindowTextLength(window);
        if (length <= 0)
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(length + 1);
        _ = NativeMethods.GetWindowText(window, buffer, buffer.Capacity);
        return buffer.ToString();
    }

    private static string? ReadClassName(nint window)
    {
        var buffer = new StringBuilder(256);
        var length = NativeMethods.GetClassName(window, buffer, buffer.Capacity);
        return length > 0 ? buffer.ToString() : null;
    }

    private static class NativeMethods
    {
        internal delegate bool EnumWindowsProc(nint window, nint parameter);

        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct Point(int x, int y)
        {
            internal readonly int X = x;
            internal readonly int Y = y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(nint window);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowTextLength(nint window);

        [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetWindowText(nint window, StringBuilder text, int maxCount);

        [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetClassName(nint window, StringBuilder className, int maxCount);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(nint window, out Rect rectangle);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(nint window, out uint processId);

        [DllImport("user32.dll")]
        internal static extern nint WindowFromPoint(Point point);

        [DllImport("user32.dll")]
        internal static extern nint GetAncestor(nint window, uint flags);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmGetWindowAttribute(
            nint window,
            int attribute,
            out int value,
            int valueSize);

        [DllImport("dwmapi.dll")]
        internal static extern int DwmGetWindowAttribute(
            nint window,
            int attribute,
            out Rect value,
            int valueSize);
    }
}
