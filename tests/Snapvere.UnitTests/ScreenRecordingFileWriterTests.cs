using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Domain.Capture;

namespace Snapvere.UnitTests;

public sealed class ScreenRecordingFileWriterTests
{
    [Fact]
    public async Task RecordAsync_PublishesCompletedMp4WithoutTemporaryFiles()
    {
        var directory = CreateTemporaryDirectory();
        var timestamp = new DateTimeOffset(2026, 9, 19, 20, 0, 0, TimeSpan.Zero);

        try
        {
            var display = CreateDisplay();
            var recorder = new FakeScreenRecordingService(timestamp);
            var writer = new ScreenRecordingFileWriter(
                recorder,
                new CapturePathProvider(directory),
                new FixedTimeProvider(timestamp));

            var result = await writer.RecordAsync(
                display,
                includeCursor: true,
                CancellationToken.None);

            Assert.Equal(
                Path.Combine(directory, "SNAPVERE_Record_2026-09-19_200000.mp4"),
                result.FilePath);
            Assert.True(File.Exists(result.FilePath));
            Assert.Equal(FakeScreenRecordingService.Payload, await File.ReadAllBytesAsync(result.FilePath));
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly));
            Assert.True(recorder.IncludeCursor);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RecordAsync_ConcurrentSameTimestampPublishesUniqueFiles()
    {
        var directory = CreateTemporaryDirectory();
        var timestamp = new DateTimeOffset(2026, 9, 19, 20, 0, 0, TimeSpan.Zero);

        try
        {
            var writer = new ScreenRecordingFileWriter(
                new FakeScreenRecordingService(timestamp),
                new CapturePathProvider(directory),
                new FixedTimeProvider(timestamp));
            var display = CreateDisplay();

            var results = await Task.WhenAll(
                writer.RecordAsync(display, false, CancellationToken.None),
                writer.RecordAsync(display, false, CancellationToken.None));

            Assert.Equal(
                2,
                results.Select(result => result.FilePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count());
            Assert.All(results, result => Assert.True(File.Exists(result.FilePath)));
            Assert.Equal(2, Directory.EnumerateFiles(directory, "*.mp4").Count());
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RecordAsync_FailureDoesNotPublishPartialRecording()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var writer = new ScreenRecordingFileWriter(
                new FailingScreenRecordingService(),
                new CapturePathProvider(directory),
                TimeProvider.System);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => writer.RecordAsync(CreateDisplay(), false, CancellationToken.None));

            Assert.Empty(Directory.EnumerateFileSystemEntries(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void BuildFileName_UsesRecordingPrefixAndCounter()
    {
        var timestamp = new DateTimeOffset(2026, 9, 19, 20, 0, 0, TimeSpan.Zero);

        Assert.Equal(
            "SNAPVERE_Record_2026-09-19_200000.mp4",
            ScreenRecordingFileWriter.BuildFileName(timestamp));
        Assert.Equal(
            "SNAPVERE_Record_2026-09-19_200000_007.mp4",
            ScreenRecordingFileWriter.BuildFileName(timestamp, 7));
    }

    private static DisplayDescriptor CreateDisplay()
        => new(
            "primary",
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            96,
            96,
            true,
            "Primary");

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "SNAPVERE-recording-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => utcNow.ToUniversalTime();
    }

    private sealed class FakeScreenRecordingService(DateTimeOffset timestamp)
        : IScreenRecordingService
    {
        internal static readonly byte[] Payload =
            [0, 0, 0, 24, 0x66, 0x74, 0x79, 0x70, 0x6D, 0x70, 0x34, 0x32];

        public bool IncludeCursor { get; private set; }

        public async Task<ScreenRecordingSessionResult> RecordDisplayAsync(
            DisplayDescriptor display,
            bool includeCursor,
            Stream destination,
            CancellationToken stopToken = default)
        {
            IncludeCursor = includeCursor;
            await destination.WriteAsync(Payload, CancellationToken.None);
            return new ScreenRecordingSessionResult(
                new PixelSize(display.Bounds.Width, display.Bounds.Height),
                new PixelSize(display.Bounds.Width, display.Bounds.Height),
                timestamp,
                timestamp.AddSeconds(2));
        }
    }

    private sealed class FailingScreenRecordingService : IScreenRecordingService
    {
        public async Task<ScreenRecordingSessionResult> RecordDisplayAsync(
            DisplayDescriptor display,
            bool includeCursor,
            Stream destination,
            CancellationToken stopToken = default)
        {
            await destination.WriteAsync(new byte[] { 1, 2, 3 }, CancellationToken.None);
            throw new InvalidOperationException("Injected recording failure.");
        }
    }
}
