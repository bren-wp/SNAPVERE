namespace Snapvere.Shared;

public readonly record struct SetupContentLayout(
    bool Compact,
    int HorizontalMargin,
    int ContentWidth,
    int CardHeight,
    int StatusHeight);

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

        var margin = mainPanelWidth < 560
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
