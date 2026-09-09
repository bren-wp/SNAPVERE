using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;

namespace Snapvere.UnitTests;

public sealed class ScreenCaptureWorkflowTests
{
    [Fact]
    public async Task CapturePrimaryDisplayToDefaultFolderAsync_WritesPngAtomically()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-tests-{Guid.NewGuid():N}");
        var timestamp = new DateTimeOffset(2026, 9, 9, 20, 45, 12, TimeSpan.FromHours(2));

        try
        {
            var primary = new DisplayDescriptor(
                "primary",
                new PixelRect(0, 0, 1, 1),
                new PixelRect(0, 0, 1, 1),
                96,
                96,
                true,
                "Primary");

            var frame = new CaptureFrame(
                new PixelSize(1, 1),
                4,
                new byte[] { 30, 20, 10, 255 },
                timestamp,
                primary.Id);

            var workflow = new ScreenCaptureWorkflow(
                new FakeDisplayDiscovery(primary),
                new FakeScreenCaptureService(frame),
                new PngCaptureEncoder(),
                new CapturePathProvider(directory),
                new FixedTimeProvider(timestamp));

            var result = await workflow.CapturePrimaryDisplayToDefaultFolderAsync(includeCursor: false);

            Assert.Equal(Path.Combine(directory, "SNAPVERE_2026-09-09_204512.png"), result.FilePath);
            Assert.True(File.Exists(result.FilePath));
            Assert.Equal(1, result.Width);
            Assert.Equal(1, result.Height);

            var bytes = await File.ReadAllBytesAsync(result.FilePath);
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes.AsSpan(0, 8).ToArray());
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
    public void BuildFileName_AddsCounterOnlyWhenNeeded()
    {
        var timestamp = new DateTimeOffset(2026, 9, 9, 20, 45, 12, TimeSpan.FromHours(2));

        Assert.Equal("SNAPVERE_2026-09-09_204512.png", CapturePathProvider.BuildFileName(timestamp));
        Assert.Equal("SNAPVERE_2026-09-09_204512_007.png", CapturePathProvider.BuildFileName(timestamp, 7));
    }

    private sealed class FakeDisplayDiscovery(DisplayDescriptor display) : IDisplayDiscovery
    {
        public IReadOnlyList<DisplayDescriptor> GetDisplays() => [display];
    }

    private sealed class FakeScreenCaptureService(CaptureFrame frame) : IScreenCaptureService
    {
        public ValueTask<CaptureFrame> CaptureDisplayAsync(
            DisplayDescriptor display,
            bool includeCursor,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(frame);
    }

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone(
            "SNAPVERE-Test",
            timestamp.Offset,
            "SNAPVERE Test",
            "SNAPVERE Test");
    }
}
