using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Windows;

public static class WindowHitTesting
{
    public static WindowDescriptor? FindTopmostAtPoint(
        IReadOnlyList<WindowDescriptor> windows,
        PixelPoint point)
    {
        ArgumentNullException.ThrowIfNull(windows);

        foreach (var window in windows)
        {
            var bounds = window.Bounds.Normalize();
            if (bounds.IsEmpty)
            {
                continue;
            }

            if (point.X >= bounds.Left && point.X < bounds.Right &&
                point.Y >= bounds.Top && point.Y < bounds.Bottom)
            {
                return window;
            }
        }

        return null;
    }
}
