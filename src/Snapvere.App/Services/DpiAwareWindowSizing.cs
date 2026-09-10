using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace Snapvere.App.Services;

/// <summary>
/// Converts WinUI design sizes expressed in DIPs to the physical-pixel sizes
/// required by AppWindow.Resize. This keeps compact secondary surfaces usable
/// on 125-200% scaled displays without changing their XAML layout metrics.
/// </summary>
internal static class DpiAwareWindowSizing
{
    private const double BaselineDpi = 96.0;

    internal static SizeInt32 ScaleSize(Window window, int widthInDips, int heightInDips)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widthInDips);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(heightInDips);

        var scale = GetScale(window);
        return new SizeInt32(
            checked((int)Math.Ceiling(widthInDips * scale)),
            checked((int)Math.Ceiling(heightInDips * scale)));
    }

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
    }
}
