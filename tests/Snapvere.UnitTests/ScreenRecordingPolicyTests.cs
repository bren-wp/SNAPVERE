using Snapvere.Capture;
using Snapvere.Domain.Capture;

namespace Snapvere.UnitTests;

public sealed class ScreenRecordingPolicyTests
{
    [Theory]
    [InlineData(1920, 1080, 1920, 1080)]
    [InlineData(1919, 1079, 1918, 1078)]
    [InlineData(2560, 1440, 2560, 1440)]
    public void EncodedSize_IsEvenAndNeverLargerThanSource(
        int width,
        int height,
        int expectedWidth,
        int expectedHeight)
    {
        var actual = ScreenRecordingPolicy.GetEncodedSize(new PixelSize(width, height));

        Assert.Equal(expectedWidth, actual.Width);
        Assert.Equal(expectedHeight, actual.Height);
    }

    [Fact]
    public void EncodedSize_RejectsSourceLargerThan8K()
    {
        Assert.Throws<NotSupportedException>(
            () => ScreenRecordingPolicy.GetEncodedSize(new PixelSize(7681, 4320)));
    }

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1920, 1080)]
    [InlineData(3840, 2160)]
    [InlineData(7680, 4320)]
    public void Bitrate_RemainsWithinProductionBounds(int width, int height)
    {
        var bitrate = ScreenRecordingPolicy.GetBitrate(new PixelSize(width, height));

        Assert.InRange(
            bitrate,
            ScreenRecordingPolicy.MinimumBitrate,
            ScreenRecordingPolicy.MaximumBitrate);
    }
}
