using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Region;

public enum SelectionHandle
{
    None,
    Body,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}

public static class RegionSelectionGeometry
{
    public static PixelRect CreateFromDrag(
        PixelPoint anchor,
        PixelPoint current,
        PixelRect desktopBounds)
    {
        var left = Math.Min(anchor.X, current.X);
        var top = Math.Min(anchor.Y, current.Y);
        var right = Math.Max(anchor.X, current.X);
        var bottom = Math.Max(anchor.Y, current.Y);

        return Clamp(
            new PixelRect(left, top, checked(right - left), checked(bottom - top)),
            desktopBounds);
    }

    public static PixelRect Move(
        PixelRect selection,
        int deltaX,
        int deltaY,
        PixelRect desktopBounds)
    {
        var normalized = selection.Normalize();
        var bounds = desktopBounds.Normalize();

        var width = Math.Min(normalized.Width, bounds.Width);
        var height = Math.Min(normalized.Height, bounds.Height);
        var maxX = checked(bounds.Right - width);
        var maxY = checked(bounds.Bottom - height);

        var x = Math.Clamp(checked(normalized.X + deltaX), bounds.Left, maxX);
        var y = Math.Clamp(checked(normalized.Y + deltaY), bounds.Top, maxY);

        return new PixelRect(x, y, width, height);
    }

    public static PixelRect Resize(
        PixelRect selection,
        SelectionHandle handle,
        int deltaX,
        int deltaY,
        PixelRect desktopBounds,
        int minimumWidth = 1,
        int minimumHeight = 1)
    {
        if (minimumWidth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumWidth));
        }

        if (minimumHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumHeight));
        }

        var rect = selection.Normalize();
        var left = rect.Left;
        var top = rect.Top;
        var right = rect.Right;
        var bottom = rect.Bottom;

        if (AffectsLeft(handle))
        {
            left = checked(left + deltaX);
        }
        else if (AffectsRight(handle))
        {
            right = checked(right + deltaX);
        }

        if (AffectsTop(handle))
        {
            top = checked(top + deltaY);
        }
        else if (AffectsBottom(handle))
        {
            bottom = checked(bottom + deltaY);
        }

        if (handle is SelectionHandle.None or SelectionHandle.Body)
        {
            return handle == SelectionHandle.Body
                ? Move(rect, deltaX, deltaY, desktopBounds)
                : rect;
        }

        EnforceMinimumSize(ref left, ref right, minimumWidth, AffectsLeft(handle));
        EnforceMinimumSize(ref top, ref bottom, minimumHeight, AffectsTop(handle));

        return Clamp(
            new PixelRect(left, top, checked(right - left), checked(bottom - top)).Normalize(),
            desktopBounds);
    }

    public static PixelRect Clamp(PixelRect selection, PixelRect desktopBounds)
    {
        var rect = selection.Normalize();
        var bounds = desktopBounds.Normalize();

        var left = Math.Clamp(rect.Left, bounds.Left, bounds.Right);
        var top = Math.Clamp(rect.Top, bounds.Top, bounds.Bottom);
        var right = Math.Clamp(rect.Right, bounds.Left, bounds.Right);
        var bottom = Math.Clamp(rect.Bottom, bounds.Top, bounds.Bottom);

        if (right < left)
        {
            (left, right) = (right, left);
        }

        if (bottom < top)
        {
            (top, bottom) = (bottom, top);
        }

        return new PixelRect(left, top, checked(right - left), checked(bottom - top));
    }

    private static void EnforceMinimumSize(
        ref int start,
        ref int end,
        int minimum,
        bool movingStart)
    {
        if (end - start >= minimum)
        {
            return;
        }

        if (movingStart)
        {
            start = checked(end - minimum);
        }
        else
        {
            end = checked(start + minimum);
        }
    }

    private static bool AffectsLeft(SelectionHandle handle)
        => handle is SelectionHandle.Left or SelectionHandle.TopLeft or SelectionHandle.BottomLeft;

    private static bool AffectsRight(SelectionHandle handle)
        => handle is SelectionHandle.Right or SelectionHandle.TopRight or SelectionHandle.BottomRight;

    private static bool AffectsTop(SelectionHandle handle)
        => handle is SelectionHandle.Top or SelectionHandle.TopLeft or SelectionHandle.TopRight;

    private static bool AffectsBottom(SelectionHandle handle)
        => handle is SelectionHandle.Bottom or SelectionHandle.BottomLeft or SelectionHandle.BottomRight;
}
