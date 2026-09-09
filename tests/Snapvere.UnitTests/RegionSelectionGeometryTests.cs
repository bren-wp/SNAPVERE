using Snapvere.Capture.Region;
using Snapvere.Domain.Capture;

namespace Snapvere.UnitTests;

public sealed class RegionSelectionGeometryTests
{
    private static readonly PixelRect Desktop = new(-1920, -1080, 4480, 2520);

    [Fact]
    public void CreateFromDrag_NormalizesReverseDrag()
    {
        var actual = RegionSelectionGeometry.CreateFromDrag(
            new PixelPoint(300, 400),
            new PixelPoint(100, 120),
            Desktop);

        Assert.Equal(new PixelRect(100, 120, 200, 280), actual);
    }

    [Fact]
    public void CreateFromDrag_ClampsToNegativeVirtualDesktopBounds()
    {
        var actual = RegionSelectionGeometry.CreateFromDrag(
            new PixelPoint(-2500, -1400),
            new PixelPoint(-1700, -900),
            Desktop);

        Assert.Equal(new PixelRect(-1920, -1080, 220, 180), actual);
    }

    [Fact]
    public void Move_PreservesSizeAndStopsAtDesktopEdge()
    {
        var source = new PixelRect(2400, 1200, 300, 200);

        var actual = RegionSelectionGeometry.Move(source, 500, 500, Desktop);

        Assert.Equal(new PixelRect(2260, 1240, 300, 200), actual);
    }

    [Fact]
    public void Resize_LeftHandleChangesOnlyHorizontalStart()
    {
        var source = new PixelRect(100, 100, 400, 300);

        var actual = RegionSelectionGeometry.Resize(
            source,
            SelectionHandle.Left,
            50,
            999,
            Desktop);

        Assert.Equal(new PixelRect(150, 100, 350, 300), actual);
    }

    [Fact]
    public void Resize_EnforcesMinimumSize()
    {
        var source = new PixelRect(100, 100, 100, 100);

        var actual = RegionSelectionGeometry.Resize(
            source,
            SelectionHandle.TopLeft,
            500,
            500,
            Desktop,
            minimumWidth: 16,
            minimumHeight: 16);

        Assert.Equal(new PixelRect(184, 184, 16, 16), actual);
    }

    [Theory]
    [InlineData(SelectionHandle.Body, 1, 0, 101, 100)]
    [InlineData(SelectionHandle.Body, 0, -1, 100, 99)]
    [InlineData(SelectionHandle.Right, 1, 0, 100, 100)]
    public void FineAdjustment_UsesSinglePhysicalPixelSteps(
        SelectionHandle handle,
        int dx,
        int dy,
        int expectedX,
        int expectedY)
    {
        var source = new PixelRect(100, 100, 100, 100);

        var actual = RegionSelectionGeometry.Resize(source, handle, dx, dy, Desktop);

        Assert.Equal(expectedX, actual.X);
        Assert.Equal(expectedY, actual.Y);
    }
}
