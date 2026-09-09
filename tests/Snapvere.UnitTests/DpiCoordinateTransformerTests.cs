using Snapvere.Capture.Geometry;
using Snapvere.Domain.Capture;

namespace Snapvere.UnitTests;

public sealed class DpiCoordinateTransformerTests
{
    [Theory]
    [InlineData(96, 100, 100)]
    [InlineData(120, 100, 125)]
    [InlineData(144, 100, 150)]
    [InlineData(192, 100, 200)]
    public void LogicalToPhysical_ScalesAtCommonWindowsDpi(uint dpi, int logical, int expected)
    {
        var actual = DpiCoordinateTransformer.LogicalToPhysical(new PixelPoint(logical, logical), dpi, dpi);

        Assert.Equal(expected, actual.X);
        Assert.Equal(expected, actual.Y);
    }

    [Fact]
    public void LogicalToPhysical_PreservesNegativeVirtualDesktopCoordinates()
    {
        var actual = DpiCoordinateTransformer.LogicalToPhysical(new PixelPoint(-1280, 240), 144, 144);

        Assert.Equal(new PixelPoint(-1920, 360), actual);
    }

    [Fact]
    public void RoundTrip_IsStableForMixedAxisDpi()
    {
        var source = new PixelRect(-300, 75, 641, 359);
        var physical = DpiCoordinateTransformer.LogicalToPhysical(source, 144, 120);
        var logical = DpiCoordinateTransformer.PhysicalToLogical(physical, 144, 120);

        Assert.Equal(source, logical);
    }
}
