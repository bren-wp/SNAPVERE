using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace Snapvere.App.Services;

/// <summary>
/// Applies the premium Obsidian window chrome to ordinary Windows settings
/// surfaces. Retains the system title bar, drag area and window commands;
/// fullscreen capture overlays and borderless recording chrome are excluded.
/// </summary>
internal static class SnapvereTitleBar
{
    public static void Apply(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // WinAppSDK supports only partial title-bar customization on Windows 10;
        // leave system chrome untouched when the platform reports unsupported.
        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

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
            System.Runtime.InteropServices.COMException)
        {
            // Decorative system chrome must never prevent the actual settings,
            // language or support window from opening on an older host.
            StartupDiagnostics.Record("Apply premium Windows title bar", exception);
        }
    }
}
