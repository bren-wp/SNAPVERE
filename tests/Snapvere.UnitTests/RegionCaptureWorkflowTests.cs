using System.Buffers.Binary;
using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;

namespace Snapvere.UnitTests;

public sealed class RegionCaptureWorkflowTests
{
    [Fact]
    public async Task SaveSelectionAsync_MapsNegativeDesktopCoordinatesAndWritesExactCrop()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-region-{Guid.NewGuid():N}");
        var timestamp = new DateTimeOffset(2026, 9, 9, 21, 10, 11, TimeSpan.FromHours(2));

        try
        {
            var display = new DisplayDescriptor(
                "left-monitor",
                new PixelRect(-4, 0, 4, 3),
                new PixelRect(-4, 0, 4, 3),
                120,
                120,
                true,
                "Left monitor");

            var frame = CreatePatternFrame(4, 3, timestamp, display.Id);
            var captureService = new FakeScreenCaptureService(frame);
            var writer = new CaptureFileWriter(
                new PngCaptureEncoder(),
                new CapturePathProvider(directory),
                new FixedTimeProvider(timestamp));
            var workflow = new RegionCaptureWorkflow(
                new FakeDisplayDiscovery(display),
                captureService,
                writer);

            var session = await workflow.PreparePrimaryDisplayAsync(includeCursor: false);
            var result = await workflow.SaveSelectionAsync(
                session,
                new PixelRect(-3, 1, 2, 1));

            Assert.Equal(2, result.Width);
            Assert.Equal(1, result.Height);
            Assert.True(File.Exists(result.FilePath));
            Assert.False(captureService.LastIncludeCursor);

            var png = await File.ReadAllBytesAsync(result.FilePath);
            Assert.Equal(2u, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16, 4)));
            Assert.Equal(1u, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20, 4)));
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PreparePrimaryDisplayAsync_RejectsFrameWhenDisplayChangedDuringCapture()
    {
        var display = new DisplayDescriptor(
            "primary",
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            96,
            96,
            true,
            "Primary");

        var mismatchedFrame = new CaptureFrame(
            new PixelSize(1280, 720),
            1280 * 4,
            new byte[1280 * 720 * 4],
            DateTimeOffset.UtcNow,
            display.Id);

        var writer = new CaptureFileWriter(
            new PngCaptureEncoder(),
            new CapturePathProvider(Path.GetTempPath()),
            TimeProvider.System);
        var workflow = new RegionCaptureWorkflow(
            new FakeDisplayDiscovery(display),
            new FakeScreenCaptureService(mismatchedFrame),
            writer);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => workflow.PreparePrimaryDisplayAsync());

        Assert.Contains("display configuration may have changed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static CaptureFrame CreatePatternFrame(
        int width,
        int height,
        DateTimeOffset capturedAt,
        string sourceId)
    {
        var stride = width * 4;
        var pixels = new byte[stride * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = (y * stride) + (x * 4);
                pixels[offset] = checked((byte)(10 + x));
                pixels[offset + 1] = checked((byte)(20 + y));
                pixels[offset + 2] = checked((byte)(30 + x + y));
                pixels[offset + 3] = 255;
            }
        }

        return new CaptureFrame(
            new PixelSize(width, height),
            stride,
            pixels,
            capturedAt,
            sourceId);
    }

    private sealed class FakeDisplayDiscovery(DisplayDescriptor display) : IDisplayDiscovery
    {
        public IReadOnlyList<DisplayDescriptor> GetDisplays() => [display];
    }

    private sealed class FakeScreenCaptureService(CaptureFrame frame) : IScreenCaptureService
    {
        public bool LastIncludeCursor { get; private set; }

        public ValueTask<CaptureFrame> CaptureDisplayAsync(
            DisplayDescriptor display,
            bool includeCursor,
            CancellationToken cancellationToken = default)
        {
            LastIncludeCursor = includeCursor;
            return ValueTask.FromResult(frame);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone(
            "SNAPVERE-Region-Test",
            timestamp.Offset,
            "SNAPVERE Region Test",
            "SNAPVERE Region Test");
    }
}
