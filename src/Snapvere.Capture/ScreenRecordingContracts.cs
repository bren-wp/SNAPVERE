using Snapvere.Domain.Capture;

namespace Snapvere.Capture;

public interface IScreenRecordingService
{
    Task<ScreenRecordingSessionResult> RecordDisplayAsync(
        DisplayDescriptor display,
        bool includeCursor,
        Stream destination,
        CancellationToken stopToken = default);
}

public sealed record ScreenRecordingSessionResult(
    PixelSize SourceSize,
    PixelSize EncodedSize,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt)
{
    public TimeSpan Duration => CompletedAt - StartedAt;
}

public static class ScreenRecordingPolicy
{
    public const uint FrameRate = 30;
    public const uint MinimumBitrate = 4_000_000;
    public const uint MaximumBitrate = 32_000_000;
    public const int MaximumLongEdge = 7680;
    public const int MaximumShortEdge = 4320;
    public const long MaximumSourcePixels = (long)MaximumLongEdge * MaximumShortEdge;

    public static PixelSize GetEncodedSize(PixelSize sourceSize)
    {
        if (sourceSize.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceSize));
        }

        if (sourceSize.Width < 2 || sourceSize.Height < 2)
        {
            throw new NotSupportedException(
                "Screen recording requires a source display of at least 2x2 pixels.");
        }

        var longEdge = Math.Max(sourceSize.Width, sourceSize.Height);
        var shortEdge = Math.Min(sourceSize.Width, sourceSize.Height);
        var pixels = checked((long)sourceSize.Width * sourceSize.Height);
        if (longEdge > MaximumLongEdge ||
            shortEdge > MaximumShortEdge ||
            pixels > MaximumSourcePixels)
        {
            throw new NotSupportedException(
                "Screen recording is limited to source displays up to 7680x4320 pixels in either orientation.");
        }

        return new PixelSize(
            sourceSize.Width & ~1,
            sourceSize.Height & ~1);
    }

    public static uint GetBitrate(PixelSize encodedSize)
    {
        if (encodedSize.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(encodedSize));
        }

        var estimated = checked((long)encodedSize.Width * encodedSize.Height * FrameRate * 8L / 100L);
        return (uint)Math.Clamp(estimated, MinimumBitrate, MaximumBitrate);
    }

    public static bool HasPublishableOutput(int deliveredFrames, ulong outputBytes)
        => deliveredFrames > 0 && outputBytes > 0;
}
