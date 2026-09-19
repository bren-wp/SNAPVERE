using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class ResponsiveWindowSizePolicyTests
{
    [Fact]
    public void FitWithinWorkArea_KeepsDesiredSizeWhenItFits()
    {
        var actual = ResponsiveWindowSizePolicy.FitWithinWorkArea(
            desiredWidth: 760,
            desiredHeight: 700,
            workAreaWidth: 1920,
            workAreaHeight: 1080,
            margin: 16);

        Assert.Equal(new ResponsiveWindowSize(760, 700), actual);
    }

    [Fact]
    public void FitWithinWorkArea_ClampsBothDimensionsInsideWorkArea()
    {
        var actual = ResponsiveWindowSizePolicy.FitWithinWorkArea(
            desiredWidth: 1520,
            desiredHeight: 1400,
            workAreaWidth: 1366,
            workAreaHeight: 728,
            margin: 16);

        Assert.Equal(new ResponsiveWindowSize(1334, 696), actual);
    }

    [Fact]
    public void FitWithinWorkArea_PreservesOnePixelWhenMarginConsumesTinyWorkArea()
    {
        var actual = ResponsiveWindowSizePolicy.FitWithinWorkArea(
            desiredWidth: 500,
            desiredHeight: 300,
            workAreaWidth: 20,
            workAreaHeight: 20,
            margin: 16);

        Assert.Equal(new ResponsiveWindowSize(1, 1), actual);
    }

    [Theory]
    [InlineData(0, 100, 100, 100, 0)]
    [InlineData(100, 0, 100, 100, 0)]
    [InlineData(100, 100, 0, 100, 0)]
    [InlineData(100, 100, 100, 0, 0)]
    [InlineData(100, 100, 100, 100, -1)]
    public void FitWithinWorkArea_RejectsInvalidGeometry(
        int desiredWidth,
        int desiredHeight,
        int workAreaWidth,
        int workAreaHeight,
        int margin)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ResponsiveWindowSizePolicy.FitWithinWorkArea(
                desiredWidth,
                desiredHeight,
                workAreaWidth,
                workAreaHeight,
                margin));
    }
}
