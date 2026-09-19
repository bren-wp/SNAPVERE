using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class SetupResponsiveLayoutPolicyTests
{
    [Theory]
    [InlineData(980, true)]
    [InlineData(900, true)]
    [InlineData(899, false)]
    [InlineData(480, false)]
    public void SidebarBreakpoint_IsDeterministic(int width, bool expected)
    {
        Assert.Equal(expected, SetupResponsiveLayoutPolicy.ShouldShowSidebar(width));
    }

    [Fact]
    public void WideMainPanel_KeepsMaximumContentWidth()
    {
        var actual = SetupResponsiveLayoutPolicy.CalculateContent(700);

        Assert.False(actual.Compact);
        Assert.Equal(32, actual.HorizontalMargin);
        Assert.Equal(636, actual.ContentWidth);
        Assert.Equal(374, actual.CardHeight);
        Assert.Equal(42, actual.StatusHeight);
    }

    [Fact]
    public void NarrowMainPanel_ReflowsWithoutHorizontalOverflow()
    {
        var actual = SetupResponsiveLayoutPolicy.CalculateContent(420);

        Assert.True(actual.Compact);
        Assert.Equal(18, actual.HorizontalMargin);
        Assert.Equal(384, actual.ContentWidth);
        Assert.Equal(410, actual.CardHeight);
        Assert.Equal(52, actual.StatusHeight);
        Assert.True(actual.ContentWidth + (2 * actual.HorizontalMargin) <= 420);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidWidths_AreRejected(int width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SetupResponsiveLayoutPolicy.ShouldShowSidebar(width));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => SetupResponsiveLayoutPolicy.CalculateContent(width));
    }
}
