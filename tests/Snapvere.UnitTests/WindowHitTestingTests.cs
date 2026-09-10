using Snapvere.Capture.Windows;
using Snapvere.Domain.Capture;

namespace Snapvere.UnitTests;

public sealed class WindowHitTestingTests
{
    [Fact]
    public void FindsFirstContainingWindowInZOrder()
    {
        var front = new WindowDescriptor(new nint(11), "Front", new PixelRect(100, 100, 400, 300), 1001, "FrontClass");
        var back = new WindowDescriptor(new nint(22), "Back", new PixelRect(0, 0, 800, 600), 1002, "BackClass");

        var result = WindowHitTesting.FindTopmostAtPoint(
            [front, back],
            new PixelPoint(150, 150));

        Assert.Same(front, result);
    }

    [Fact]
    public void FallsThroughWhenFrontWindowDoesNotContainPoint()
    {
        var front = new WindowDescriptor(new nint(11), "Front", new PixelRect(100, 100, 200, 200), 1001, "FrontClass");
        var back = new WindowDescriptor(new nint(22), "Back", new PixelRect(-500, -300, 1200, 900), 1002, "BackClass");

        var result = WindowHitTesting.FindTopmostAtPoint(
            [front, back],
            new PixelPoint(-120, 20));

        Assert.Same(back, result);
    }

    [Fact]
    public void UsesRightAndBottomAsExclusiveEdges()
    {
        var window = new WindowDescriptor(new nint(33), "Window", new PixelRect(10, 20, 100, 50), 1003, null);

        Assert.Same(window, WindowHitTesting.FindTopmostAtPoint([window], new PixelPoint(109, 69)));
        Assert.Null(WindowHitTesting.FindTopmostAtPoint([window], new PixelPoint(110, 69)));
        Assert.Null(WindowHitTesting.FindTopmostAtPoint([window], new PixelPoint(109, 70)));
    }

    [Fact]
    public void IgnoresEmptyWindows()
    {
        var empty = new WindowDescriptor(new nint(44), "Empty", new PixelRect(0, 0, 0, 500), 1004, null);

        Assert.Null(WindowHitTesting.FindTopmostAtPoint([empty], new PixelPoint(0, 0)));
    }
}
