namespace Snapvere.Shared;

/// <summary>
/// Computes a compact tray-popup origin near the notification-area invocation
/// point while keeping the complete surface inside the active work area.
/// </summary>
public static class TrayPopupPlacementPolicy
{
    public static TrayPopupPlacement Place(
        int cursorX,
        int cursorY,
        int popupWidth,
        int popupHeight,
        int workAreaLeft,
        int workAreaTop,
        int workAreaRight,
        int workAreaBottom,
        int edgeMargin = 6,
        int cursorGap = 4,
        int horizontalAnchorOffset = 20)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(popupWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(popupHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(edgeMargin);
        ArgumentOutOfRangeException.ThrowIfNegative(cursorGap);
        ArgumentOutOfRangeException.ThrowIfNegative(horizontalAnchorOffset);

        if (workAreaRight <= workAreaLeft)
        {
            throw new ArgumentOutOfRangeException(nameof(workAreaRight));
        }

        if (workAreaBottom <= workAreaTop)
        {
            throw new ArgumentOutOfRangeException(nameof(workAreaBottom));
        }

        var minX = checked(workAreaLeft + edgeMargin);
        var maxX = Math.Max(
            minX,
            checked(workAreaRight - popupWidth - edgeMargin));
        var minY = checked(workAreaTop + edgeMargin);
        var maxY = Math.Max(
            minY,
            checked(workAreaBottom - popupHeight - edgeMargin));

        var preferredX = checked(cursorX - popupWidth + horizontalAnchorOffset);

        var spaceAbove = Math.Max(0, cursorY - minY);
        var spaceBelow = Math.Max(0, maxY + popupHeight - cursorY);
        var openAbove = spaceAbove >= popupHeight + cursorGap ||
                        spaceAbove >= spaceBelow;

        var preferredY = openAbove
            ? checked(cursorY - popupHeight - cursorGap)
            : checked(cursorY + cursorGap);

        return new TrayPopupPlacement(
            Math.Clamp(preferredX, minX, maxX),
            Math.Clamp(preferredY, minY, maxY));
    }
}

public readonly record struct TrayPopupPlacement(int X, int Y);
