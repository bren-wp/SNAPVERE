using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Geometry;

public static class VirtualDesktopLayout
{
    public static PixelRect GetBounds(IReadOnlyCollection<DisplayDescriptor> displays)
    {
        ArgumentNullException.ThrowIfNull(displays);

        if (displays.Count == 0)
        {
            throw new ArgumentException("At least one display is required.", nameof(displays));
        }

        var left = int.MaxValue;
        var top = int.MaxValue;
        var right = int.MinValue;
        var bottom = int.MinValue;

        foreach (var display in displays)
        {
            var bounds = display.Bounds.Normalize();
            left = Math.Min(left, bounds.Left);
            top = Math.Min(top, bounds.Top);
            right = Math.Max(right, bounds.Right);
            bottom = Math.Max(bottom, bounds.Bottom);
        }

        return new PixelRect(
            left,
            top,
            checked(right - left),
            checked(bottom - top));
    }

    public static DisplayDescriptor? FindDisplayAt(
        IReadOnlyCollection<DisplayDescriptor> displays,
        PixelPoint point)
    {
        ArgumentNullException.ThrowIfNull(displays);

        foreach (var display in displays)
        {
            var bounds = display.Bounds.Normalize();
            if (point.X >= bounds.Left && point.X < bounds.Right &&
                point.Y >= bounds.Top && point.Y < bounds.Bottom)
            {
                return display;
            }
        }

        return null;
    }
}
