namespace Snapvere.Shared;

public readonly record struct SetupContentLayout(
    bool Compact,
    int HorizontalMargin,
    int ContentWidth,
    int CardHeight,
    int StatusHeight);

/// <summary>
/// Pure responsive sizing rules for the Windows Setup surface. The WinForms
/// layer supplies its actual client/main-panel widths and applies these values
/// to controls; keeping the breakpoint math here makes narrow-window behavior
/// deterministic and regression-testable without creating a live Form.
/// </summary>
public static class SetupResponsiveLayoutPolicy
{
    public const int SidebarBreakpoint = 900;
    public const int WideHorizontalMargin = 32;
    public const int CompactHorizontalMargin = 18;
    public const int MaximumContentWidth = 636;
    public const int CompactContentBreakpoint = 520;

    public static bool ShouldShowSidebar(int clientWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clientWidth);
        return clientWidth >= SidebarBreakpoint;
    }

    public static SetupContentLayout CalculateContent(int mainPanelWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mainPanelWidth);

        var compactMarginThreshold = 560;
        var margin = mainPanelWidth < compactMarginThreshold
            ? CompactHorizontalMargin
            : WideHorizontalMargin;
        var available = Math.Max(1, mainPanelWidth - (2 * margin));
        var contentWidth = Math.Min(MaximumContentWidth, available);
        var compact = contentWidth < CompactContentBreakpoint;

        return new SetupContentLayout(
            compact,
            margin,
            contentWidth,
            compact ? 410 : 374,
            compact ? 52 : 42);
    }
}
