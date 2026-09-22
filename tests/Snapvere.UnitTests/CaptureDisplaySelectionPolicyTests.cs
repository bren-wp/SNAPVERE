using Snapvere.Capture;
using Snapvere.Domain.Capture;

namespace Snapvere.UnitTests;

public sealed class CaptureDisplaySelectionPolicyTests
{
    private static readonly DisplayDescriptor Primary = new(
        "primary",
        new PixelRect(0, 0, 1920, 1080),
        new PixelRect(0, 0, 1920, 1040),
        96,
        96,
        true,
        "Primary");

    private static readonly DisplayDescriptor Left = new(
        "left",
        new PixelRect(-1280, 0, 1280, 1024),
        new PixelRect(-1280, 0, 1280, 984),
        120,
        120,
        false,
        "Left");

    [Fact]
    public void SelectForPoint_UsesDisplayContainingCursor()
    {
        var selected = CaptureDisplaySelectionPolicy.SelectForPoint(
            [Primary, Left],
            new PixelPoint(-640, 400));

        Assert.Same(Left, selected);
    }

    [Fact]
    public void SelectForPoint_UsesHalfOpenMonitorBounds()
    {
        Assert.Same(
            Primary,
            CaptureDisplaySelectionPolicy.SelectForPoint(
                [Primary, Left],
                new PixelPoint(0, 0)));
    }

    [Fact]
    public void SelectForPoint_FallsBackToPrimaryOutsideKnownDisplays()
    {
        Assert.Same(
            Primary,
            CaptureDisplaySelectionPolicy.SelectForPoint(
                [Left, Primary],
                new PixelPoint(5000, 5000)));
    }

    [Fact]
    public void SelectPrimary_FallsBackToFirstWhenPrimaryFlagIsUnavailable()
    {
        var first = Left with { IsPrimary = false };
        var second = Primary with { IsPrimary = false };

        Assert.Same(first, CaptureDisplaySelectionPolicy.SelectPrimary([first, second]));
    }

    [Fact]
    public void Selection_RejectsEmptyDisplaySet()
    {
        Assert.Throws<InvalidOperationException>(
            () => CaptureDisplaySelectionPolicy.SelectPrimary([]));
        Assert.Throws<InvalidOperationException>(
            () => CaptureDisplaySelectionPolicy.SelectForPoint([], new PixelPoint(0, 0)));
    }
}
