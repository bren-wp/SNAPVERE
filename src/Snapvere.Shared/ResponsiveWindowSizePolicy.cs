namespace Snapvere.Shared;

public readonly record struct ResponsiveWindowSize(int Width, int Height);

/// <summary>
/// Pure sizing policy for fitting a desired window inside the usable monitor
/// work area while retaining a small outer margin. Platform-specific UI layers
/// provide DPI-scaled pixel measurements; this policy only performs the bounded
/// geometry decision so it can be regression-tested without a live HWND.
/// </summary>
public static class ResponsiveWindowSizePolicy
{
    public static ResponsiveWindowSize FitWithinWorkArea(
        int desiredWidth,
        int desiredHeight,
        int workAreaWidth,
        int workAreaHeight,
        int margin)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(desiredWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(desiredHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workAreaWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workAreaHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(margin);

        var availableWidth = Math.Max(1L, (long)workAreaWidth - (2L * margin));
        var availableHeight = Math.Max(1L, (long)workAreaHeight - (2L * margin));

        return new ResponsiveWindowSize(
            Math.Min(desiredWidth, checked((int)Math.Min(int.MaxValue, availableWidth))),
            Math.Min(desiredHeight, checked((int)Math.Min(int.MaxValue, availableHeight))));
    }
}
