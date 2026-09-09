using Snapvere.Domain.Capture;

namespace Snapvere.Capture;

public interface IDisplayDiscovery
{
    IReadOnlyList<DisplayDescriptor> GetDisplays();
}

public interface IScreenCaptureService
{
    ValueTask<CaptureFrame> CaptureDisplayAsync(
        DisplayDescriptor display,
        bool includeCursor,
        CancellationToken cancellationToken = default);
}

public sealed record CaptureFrame(
    PixelSize Size,
    int Stride,
    ReadOnlyMemory<byte> Bgra8Pixels,
    DateTimeOffset CapturedAt,
    string? SourceId)
{
    public int ExpectedByteLength => checked(Stride * Size.Height);

    public void Validate()
    {
        if (Size.IsEmpty)
        {
            throw new InvalidOperationException("A capture frame cannot have an empty size.");
        }

        if (Stride < checked(Size.Width * 4))
        {
            throw new InvalidOperationException("Capture stride is smaller than the BGRA8 row size.");
        }

        if (Bgra8Pixels.Length < ExpectedByteLength)
        {
            throw new InvalidOperationException("Capture pixel buffer is smaller than the declared frame geometry.");
        }
    }
}
