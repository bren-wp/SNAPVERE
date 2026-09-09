using Snapvere.Capture;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;

namespace Snapvere.UnitTests;

public sealed class CaptureFrameCropperTests
{
    [Fact]
    public void Crop_CopiesExactBgraPixelsAcrossSourceStride()
    {
        var source = new CaptureFrame(
            new PixelSize(3, 2),
            16,
            new byte[]
            {
                1, 2, 3, 255,  4, 5, 6, 255,  7, 8, 9, 255,  99, 99, 99, 99,
                10, 11, 12, 255,  13, 14, 15, 255,  16, 17, 18, 255,  88, 88, 88, 88
            },
            DateTimeOffset.UnixEpoch,
            "source");

        var actual = CaptureFrameCropper.Crop(source, new PixelRect(1, 0, 2, 2));

        Assert.Equal(new PixelSize(2, 2), actual.Size);
        Assert.Equal(8, actual.Stride);
        Assert.Equal(
            new byte[]
            {
                4, 5, 6, 255,  7, 8, 9, 255,
                13, 14, 15, 255,  16, 17, 18, 255
            },
            actual.Bgra8Pixels.ToArray());
    }

    [Fact]
    public void Crop_RejectsRegionOutsideFrame()
    {
        var source = new CaptureFrame(
            new PixelSize(100, 100),
            400,
            new byte[40_000],
            DateTimeOffset.UnixEpoch,
            "source");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CaptureFrameCropper.Crop(source, new PixelRect(90, 90, 20, 20)));
    }
}
