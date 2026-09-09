using Snapvere.Capture;
using Snapvere.Domain.Capture;

namespace Snapvere.Imaging;

public static class CaptureFrameCropper
{
    private const int BytesPerPixel = 4;

    public static CaptureFrame Crop(CaptureFrame source, PixelRect region)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.Validate();

        var normalized = region.Normalize();
        if (normalized.IsEmpty)
        {
            throw new ArgumentException("Crop region must be non-empty.", nameof(region));
        }

        if (normalized.Left < 0 ||
            normalized.Top < 0 ||
            normalized.Right > source.Size.Width ||
            normalized.Bottom > source.Size.Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(region),
                "Crop region must fit completely inside the source frame.");
        }

        var destinationStride = checked(normalized.Width * BytesPerPixel);
        var destination = new byte[checked(destinationStride * normalized.Height)];
        var sourcePixels = source.Bgra8Pixels.Span;

        for (var row = 0; row < normalized.Height; row++)
        {
            var sourceOffset = checked(
                (normalized.Top + row) * source.Stride +
                normalized.Left * BytesPerPixel);
            var destinationOffset = checked(row * destinationStride);

            sourcePixels
                .Slice(sourceOffset, destinationStride)
                .CopyTo(destination.AsSpan(destinationOffset, destinationStride));
        }

        var cropped = new CaptureFrame(
            new PixelSize(normalized.Width, normalized.Height),
            destinationStride,
            destination,
            source.CapturedAt,
            source.SourceId);

        cropped.Validate();
        return cropped;
    }
}
