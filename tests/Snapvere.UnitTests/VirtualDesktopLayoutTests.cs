using Snapvere.Capture.Geometry;
using Snapvere.Domain.Capture;

namespace Snapvere.UnitTests;

public sealed class VirtualDesktopLayoutTests
{
    [Fact]
    public void GetBounds_HandlesDisplaysLeftAndAbovePrimary()
    {
        DisplayDescriptor[] displays =
        [
            Display("left", new PixelRect(-1920, 0, 1920, 1080), false),
            Display("primary", new PixelRect(0, 0, 2560, 1440), true),
            Display("above", new PixelRect(2560, -1600, 1200, 1600), false)
        ];

        var actual = VirtualDesktopLayout.GetBounds(displays);

        Assert.Equal(new PixelRect(-1920, -1600, 5680, 3040), actual);
    }

    [Fact]
    public void FindDisplayAt_UsesRightAndBottomAsExclusiveEdges()
    {
        var left = Display("left", new PixelRect(-100, 0, 100, 100), false);
        var primary = Display("primary", new PixelRect(0, 0, 100, 100), true);
        DisplayDescriptor[] displays = [left, primary];

        Assert.Same(left, VirtualDesktopLayout.FindDisplayAt(displays, new PixelPoint(-1, 50)));
        Assert.Same(primary, VirtualDesktopLayout.FindDisplayAt(displays, new PixelPoint(0, 50)));
        Assert.Null(VirtualDesktopLayout.FindDisplayAt(displays, new PixelPoint(100, 50)));
    }

    [Fact]
    public void GetBounds_RejectsEmptyDisplaySet()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            VirtualDesktopLayout.GetBounds(Array.Empty<DisplayDescriptor>()));

        Assert.Equal("displays", exception.ParamName);
    }

    private static DisplayDescriptor Display(string id, PixelRect bounds, bool primary)
        => new(id, bounds, bounds, 96, 96, primary, id);
}
