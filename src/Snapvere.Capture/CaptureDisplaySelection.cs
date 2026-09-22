using Snapvere.Domain.Capture;

namespace Snapvere.Capture;

public interface ICaptureDisplaySelector
{
    DisplayDescriptor SelectDisplay(IReadOnlyList<DisplayDescriptor> displays);
}

public static class CaptureDisplaySelectionPolicy
{
    public static DisplayDescriptor SelectPrimary(IReadOnlyList<DisplayDescriptor> displays)
    {
        ArgumentNullException.ThrowIfNull(displays);
        if (displays.Count == 0)
        {
            throw new InvalidOperationException("SNAPVERE could not find an active display.");
        }

        return displays.FirstOrDefault(display => display.IsPrimary) ?? displays[0];
    }

    public static DisplayDescriptor SelectForPoint(
        IReadOnlyList<DisplayDescriptor> displays,
        PixelPoint point)
    {
        ArgumentNullException.ThrowIfNull(displays);
        if (displays.Count == 0)
        {
            throw new InvalidOperationException("SNAPVERE could not find an active display.");
        }

        foreach (var display in displays)
        {
            var bounds = display.Bounds.Normalize();
            if (point.X >= bounds.Left &&
                point.X < bounds.Right &&
                point.Y >= bounds.Top &&
                point.Y < bounds.Bottom)
            {
                return display;
            }
        }

        return SelectPrimary(displays);
    }
}
