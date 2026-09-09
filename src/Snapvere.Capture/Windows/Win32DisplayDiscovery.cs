using System.ComponentModel;
using System.Runtime.InteropServices;
using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Windows;

public sealed class Win32DisplayDiscovery : IDisplayDiscovery
{
    private const uint MonitorInfoPrimary = 0x00000001;
    private const int EffectiveDpi = 0;
    private const uint DefaultDpi = 96;

    public IReadOnlyList<DisplayDescriptor> GetDisplays()
    {
        var displays = new List<DisplayDescriptor>();
        Exception? callbackError = null;

        NativeMethods.MonitorEnumProc callback = (monitor, _, _, _) =>
        {
            try
            {
                var info = new NativeMethods.MonitorInfoEx
                {
                    Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfoEx>(),
                    DeviceName = string.Empty
                };

                if (!NativeMethods.GetMonitorInfo(monitor, ref info))
                {
                    callbackError = new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "GetMonitorInfo failed while enumerating displays.");
                    return false;
                }

                var (dpiX, dpiY) = GetMonitorDpi(monitor);
                var id = string.IsNullOrWhiteSpace(info.DeviceName)
                    ? $"monitor:{monitor:X}"
                    : info.DeviceName;

                displays.Add(new DisplayDescriptor(
                    id,
                    ToPixelRect(info.Monitor),
                    ToPixelRect(info.WorkArea),
                    dpiX,
                    dpiY,
                    (info.Flags & MonitorInfoPrimary) != 0,
                    string.IsNullOrWhiteSpace(info.DeviceName) ? null : info.DeviceName));

                return true;
            }
            catch (Exception exception)
            {
                callbackError = exception;
                return false;
            }
        };

        var success = NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);

        if (callbackError is not null)
        {
            throw new InvalidOperationException("Display enumeration failed inside the monitor callback.", callbackError);
        }

        if (!success)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "EnumDisplayMonitors failed.");
        }

        return displays;
    }

    private static (uint DpiX, uint DpiY) GetMonitorDpi(nint monitor)
    {
        var result = NativeMethods.GetDpiForMonitor(monitor, EffectiveDpi, out var dpiX, out var dpiY);
        return result >= 0 && dpiX > 0 && dpiY > 0
            ? (dpiX, dpiY)
            : (DefaultDpi, DefaultDpi);
    }

    private static PixelRect ToPixelRect(NativeMethods.Rect rect)
        => new(rect.Left, rect.Top, checked(rect.Right - rect.Left), checked(rect.Bottom - rect.Top));

    private static class NativeMethods
    {
        internal delegate bool MonitorEnumProc(nint monitor, nint hdcMonitor, nint monitorRect, nint data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MonitorInfoEx
        {
            internal uint Size;
            internal Rect Monitor;
            internal Rect WorkArea;
            internal uint Flags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string DeviceName;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumDisplayMonitors(
            nint hdc,
            nint clipRect,
            MonitorEnumProc callback,
            nint data);

        [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(nint monitor, ref MonitorInfoEx monitorInfo);

        [DllImport("shcore.dll")]
        internal static extern int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);
    }
}
