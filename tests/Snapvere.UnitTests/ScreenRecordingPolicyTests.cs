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

    [Theory]
    [InlineData(7681, 1000)]
    [InlineData(1000, 7681)]
    [InlineData(5000, 5000)]
    public void EncodedSize_RejectsSourceOutside8KEnvelope(int width, int height)
    {
        Assert.Throws<NotSupportedException>(
            () => ScreenRecordingPolicy.GetEncodedSize(new PixelSize(width, height)));
    }

    [Theory]
    [InlineData(7680, 4320)]
    [InlineData(4320, 7680)]
    public void EncodedSize_Accepts8KInEitherOrientation(int width, int height)
    {
        var actual = ScreenRecordingPolicy.GetEncodedSize(new PixelSize(width, height));

        Assert.Equal(width, actual.Width);
        Assert.Equal(height, actual.Height);
    }

    [Theory]
    [InlineData(1, 1080)]
    [InlineData(1920, 1)]
    public void EncodedSize_RejectsSourceThatCannotRemainEvenWithoutUpscaling(
        int width,
        int height)
    {
        Assert.Throws<NotSupportedException>(
            () => ScreenRecordingPolicy.GetEncodedSize(new PixelSize(width, height)));
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(30, 4096, true)]
    [InlineData(0, 4096, false)]
    [InlineData(30, 0, false)]
    public void PublishableOutput_RequiresFrameAndBytes(
        int deliveredFrames,
        ulong outputBytes,
        bool expected)
    {
        Assert.Equal(
            expected,
            ScreenRecordingPolicy.HasPublishableOutput(deliveredFrames, outputBytes));
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
