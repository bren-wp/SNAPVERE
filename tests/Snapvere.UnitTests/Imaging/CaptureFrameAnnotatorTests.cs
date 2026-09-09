using Snapvere.Capture;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;

namespace Snapvere.UnitTests.Imaging;

public sealed class CaptureFrameAnnotatorTests
{
    [Fact]
    public void Apply_LineRasterizesIntoBgraFrame()
    {
        var frame = CreateOpaqueFrame(12, 8, red: 10, green: 20, blue: 30);
        var annotation = new CaptureAnnotation(
            CaptureAnnotationKind.Line,
            [new CaptureAnnotationPoint(2, 4), new CaptureAnnotationPoint(9, 4)],
            CaptureAnnotationColor.Coral,
            3);

        var result = CaptureFrameAnnotator.Apply(frame, [annotation]);
        var pixel = PixelAt(result, 5, 4);

        Assert.Equal(CaptureAnnotationColor.Coral.Red, pixel.Red);
        Assert.Equal(CaptureAnnotationColor.Coral.Green, pixel.Green);
        Assert.Equal(CaptureAnnotationColor.Coral.Blue, pixel.Blue);
        Assert.Equal(byte.MaxValue, pixel.Alpha);
    }

    [Fact]
    public void Apply_HighlightAlphaBlendsInsteadOfReplacingSource()
    {
        var frame = CreateOpaqueFrame(8, 8, red: 20, green: 40, blue: 60);
        var annotation = new CaptureAnnotation(
            CaptureAnnotationKind.Highlight,
            [new CaptureAnnotationPoint(1, 4), new CaptureAnnotationPoint(6, 4)],
            CaptureAnnotationColor.Amber,
            5);

        var result = CaptureFrameAnnotator.Apply(frame, [annotation]);
        var pixel = PixelAt(result, 4, 4);

        Assert.InRange(pixel.Red, (byte)21, (byte)254);
        Assert.InRange(pixel.Green, (byte)41, (byte)254);
        Assert.InRange(pixel.Blue, (byte)1, (byte)254);
        Assert.Equal(byte.MaxValue, pixel.Alpha);
    }

    [Fact]
    public void Apply_RectangleClipsSafelyAtFrameEdges()
    {
        var frame = CreateOpaqueFrame(10, 10, red: 0, green: 0, blue: 0);
        var annotation = new CaptureAnnotation(
            CaptureAnnotationKind.Rectangle,
            [new CaptureAnnotationPoint(-4, -4), new CaptureAnnotationPoint(7, 7)],
            CaptureAnnotationColor.Mint,
            4);

        var result = CaptureFrameAnnotator.Apply(frame, [annotation]);

        result.Validate();
        Assert.Equal(frame.Size, result.Size);
        Assert.NotEqual(frame.Bgra8Pixels.ToArray(), result.Bgra8Pixels.ToArray());
    }

    [Fact]
    public void Apply_EmptyAnnotationListReturnsOriginalFrame()
    {
        var frame = CreateOpaqueFrame(4, 4, red: 1, green: 2, blue: 3);

        var result = CaptureFrameAnnotator.Apply(frame, []);

        Assert.Same(frame, result);
    }

    private static CaptureFrame CreateOpaqueFrame(int width, int height, byte red, byte green, byte blue)
    {
        var stride = checked(width * 4);
        var pixels = new byte[checked(stride * height)];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = blue;
            pixels[index + 1] = green;
            pixels[index + 2] = red;
            pixels[index + 3] = byte.MaxValue;
        }

        return new CaptureFrame(
            new PixelSize(width, height),
            stride,
            pixels,
            DateTimeOffset.UtcNow,
            "test");
    }

    private static (byte Red, byte Green, byte Blue, byte Alpha) PixelAt(CaptureFrame frame, int x, int y)
    {
        var offset = checked(y * frame.Stride + x * 4);
        var pixels = frame.Bgra8Pixels.Span;
        return (pixels[offset + 2], pixels[offset + 1], pixels[offset], pixels[offset + 3]);
    }
}
