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
    public const long MaximumSourcePixels = 7680L * 4320L;

    public static PixelSize GetEncodedSize(PixelSize sourceSize)
    {
        if (sourceSize.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceSize));
        }

        var pixels = checked((long)sourceSize.Width * sourceSize.Height);
        if (pixels > MaximumSourcePixels)
        {
            throw new NotSupportedException(
                "Screen recording is limited to source displays up to 7680x4320 pixels.");
        }

        return new PixelSize(
            Math.Max(2, sourceSize.Width & ~1),
            Math.Max(2, sourceSize.Height & ~1));
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
