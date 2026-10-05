using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class RegionOverlayLayoutPolicyTests
{
    [Fact]
    public void FitToolPaletteHeight_KeepsPreferredHeightOnNormalDesktop()
    {
        var actual = RegionOverlayLayoutPolicy.FitToolPaletteHeight(
            overlayHeight: 1080d,
            preferredHeight: 327d);

        Assert.Equal(327d, actual);
    }

    [Fact]
    public void FitToolPaletteHeight_ClampsToShortHighDpiOverlay()
    {
        var actual = RegionOverlayLayoutPolicy.FitToolPaletteHeight(
            overlayHeight: 341d,
            preferredHeight: 327d);

        Assert.Equal(325d, actual);
    }

    [Fact]
    public void FitToolPaletteHeight_PreservesOneDipOnTinyOverlay()
    {
        var actual = RegionOverlayLayoutPolicy.FitToolPaletteHeight(
            overlayHeight: 12d,
            preferredHeight: 327d);

        Assert.Equal(1d, actual);
    }

    [Theory]
    [InlineData(-1d, 327d, 8d)]
    [InlineData(100d, 0d, 8d)]
    [InlineData(100d, -1d, 8d)]
    [InlineData(100d, 327d, -1d)]
    public void FitToolPaletteHeight_RejectsInvalidGeometry(
        double overlayHeight,
        double preferredHeight,
        double edgeMargin)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RegionOverlayLayoutPolicy.FitToolPaletteHeight(
                overlayHeight,
                preferredHeight,
                edgeMargin));
    }
}
