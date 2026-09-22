using System.Runtime.InteropServices;
using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Windows;

/// <summary>
/// Selects the interactive capture display from the current cursor position.
/// If Windows cannot provide a cursor position, selection fails safely to the
/// primary display contract.
/// </summary>
public sealed class Win32CursorCaptureDisplaySelector : ICaptureDisplaySelector
{
    public DisplayDescriptor SelectDisplay(IReadOnlyList<DisplayDescriptor> displays)
    {
        ArgumentNullException.ThrowIfNull(displays);
        if (displays.Count == 0)
        {
            throw new InvalidOperationException("SNAPVERE could not find an active display.");
        }

        return NativeMethods.GetCursorPos(out var cursor)
            ? CaptureDisplaySelectionPolicy.SelectForPoint(
                displays,
                new PixelPoint(cursor.X, cursor.Y))
            : CaptureDisplaySelectionPolicy.SelectPrimary(displays);
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            internal int X;
            internal int Y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out Point point);
    }
}
