using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace Snapvere.App.Services;

/// <summary>
/// Applies dark, brand-aligned system chrome to ordinary SNAPVERE windows.
/// Windows 11 gets a DWM caption-color fallback where WinAppSDK title-bar
/// colors are not honored; older Windows versions keep their supported chrome.
/// Fullscreen capture and borderless recording overlays are excluded.
/// </summary>
internal static class SnapvereTitleBar
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(
        nint windowHandle,
        int attribute,
        ref int value,
        int valueSize);

    public static void Apply(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // WinUI may create its HWND or finalize the title bar at activation.
        // Reapply after activation so early constructor-only calls cannot
        // silently leave a bright system caption on a dark application.
        window.Activated += (_, _) => ApplyChrome(window);
        ApplyChrome(window);
    }

    private static void ApplyChrome(Window window)
    {
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            try
            {
                var titleBar = window.AppWindow.TitleBar;
                titleBar.BackgroundColor = SnapvereBrand.Obsidian.Color;
                titleBar.ForegroundColor = SnapvereBrand.Strong.Color;
                titleBar.InactiveBackgroundColor = SnapvereBrand.Obsidian.Color;
                titleBar.InactiveForegroundColor = SnapvereBrand.Muted.Color;

                titleBar.ButtonBackgroundColor = SnapvereBrand.Obsidian.Color;
                titleBar.ButtonForegroundColor = SnapvereBrand.Strong.Color;
                titleBar.ButtonHoverBackgroundColor = SnapvereBrand.Slate.Color;
                titleBar.ButtonHoverForegroundColor = SnapvereBrand.Strong.Color;
                titleBar.ButtonPressedBackgroundColor = SnapvereBrand.Violet.Color;
                titleBar.ButtonPressedForegroundColor = SnapvereBrand.Strong.Color;
                titleBar.ButtonInactiveBackgroundColor = SnapvereBrand.Obsidian.Color;
                titleBar.ButtonInactiveForegroundColor = SnapvereBrand.Muted.Color;
            }
            catch (Exception exception) when (
                exception is InvalidOperationException or
                NotSupportedException or
                COMException)
            {
                StartupDiagnostics.Record("Apply premium Windows title bar", exception);
            }
        }

        ApplyDwmCaption(window);
    }

    private static void ApplyDwmCaption(Window window)
    {
        try
        {
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (windowHandle == nint.Zero)
            {
                return;
            }

            var enabled = 1;
            if (DwmSetWindowAttribute(
                    windowHandle, DwmwaUseImmersiveDarkMode, ref enabled, sizeof(int)) < 0)
            {
                // Earlier Windows 10 builds used attribute 19. Failures are
                // expected on unsupported builds and should not block the UI.
                _ = DwmSetWindowAttribute(
                    windowHandle, DwmwaUseImmersiveDarkModeLegacy, ref enabled, sizeof(int));
            }

            // Available on Windows 11; ignored when Windows 10 reports an
            // unsupported attribute. COLORREF stores RGB bytes in BGR order.
            var background = SnapvereBrand.Obsidian.Color;
            var captionColor = background.R | (background.G << 8) | (background.B << 16);
            _ = DwmSetWindowAttribute(
                windowHandle, DwmwaCaptionColor, ref captionColor, sizeof(int));

            var foreground = SnapvereBrand.Strong.Color;
            var textColor = foreground.R | (foreground.G << 8) | (foreground.B << 16);
            _ = DwmSetWindowAttribute(
                windowHandle, DwmwaTextColor, ref textColor, sizeof(int));
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
            NotSupportedException or
            COMException or
            DllNotFoundException or
            EntryPointNotFoundException)
        {
            StartupDiagnostics.Record("Apply system dark caption", exception);
        }
    }
}
