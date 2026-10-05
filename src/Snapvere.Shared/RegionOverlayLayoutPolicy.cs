namespace Snapvere.Shared;

/// <summary>
/// Keeps the Region Capture annotation tool rail inside short logical work areas
/// while preserving its full preferred height on normal desktop displays.
/// </summary>
public static class RegionOverlayLayoutPolicy
{
    public static double FitToolPaletteHeight(
        double overlayHeight,
        double preferredHeight,
        double edgeMargin = 8d)
    {
        if (!double.IsFinite(overlayHeight) || overlayHeight < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(overlayHeight));
        }

        if (!double.IsFinite(preferredHeight) || preferredHeight <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(preferredHeight));
        }

        if (!double.IsFinite(edgeMargin) || edgeMargin < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(edgeMargin));
        }

        var availableHeight = Math.Max(1d, overlayHeight - edgeMargin * 2d);
        return Math.Min(preferredHeight, availableHeight);
    }
}
