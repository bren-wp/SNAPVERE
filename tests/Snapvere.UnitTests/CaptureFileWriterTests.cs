using Snapvere.Application.Capture;
using Snapvere.Capture;
using Snapvere.Domain.Capture;
using Snapvere.Imaging;

namespace Snapvere.UnitTests;

public sealed class CaptureFileWriterTests
{
    [Fact]
    public async Task SavePngAsync_WritesCompletedPngWithoutLeavingTemporaryFiles()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var writer = new CaptureFileWriter(
                new PngCaptureEncoder(),
                new CapturePathProvider(directory),
                TimeProvider.System);

            var result = await writer.SavePngAsync(CreateFrame());

            Assert.True(File.Exists(result.FilePath));
            Assert.EndsWith(".png", result.FilePath, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                Directory.EnumerateFiles(directory),
                path => path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SavePngAsync_ConcurrentSameTimestampPublishesUniqueFiles()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var fixedTime = new DateTimeOffset(2026, 9, 18, 18, 0, 0, TimeSpan.Zero);
            var writer = new CaptureFileWriter(
                new PngCaptureEncoder(),
                new CapturePathProvider(directory),
                new FixedTimeProvider(fixedTime));

            var first = writer.SavePngAsync(CreateFrame());
            var second = writer.SavePngAsync(CreateFrame());
            var results = await Task.WhenAll(first, second);

            Assert.Equal(2, results.Select(result => result.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(results, result => Assert.True(File.Exists(result.FilePath)));
            Assert.Equal(2, Directory.EnumerateFiles(directory, "*.png", SearchOption.TopDirectoryOnly).Count());
            Assert.DoesNotContain(
                Directory.EnumerateFiles(directory),
                path => path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }


    [Fact]
    public async Task SavePngAsync_RemovesOnlyStaleOwnedTemporaryFiles()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var fixedTime = new DateTimeOffset(2026, 9, 19, 0, 0, 0, TimeSpan.Zero);
            var staleOwned = Path.Combine(
                directory,
                $".SNAPVERE_2026-09-17_120000.png.{Guid.NewGuid():N}.tmp");
            var recentOwned = Path.Combine(
                directory,
                $".SNAPVERE_2026-09-18_235500.png.{Guid.NewGuid():N}.tmp");
            var foreignSimilar = Path.Combine(
                directory,
                ".SNAPVERE_manual-note.png.not-a-guid.tmp");

            File.WriteAllText(staleOwned, "stale");
            File.WriteAllText(recentOwned, "recent");
            File.WriteAllText(foreignSimilar, "foreign");
            File.SetLastWriteTimeUtc(staleOwned, fixedTime.UtcDateTime.AddDays(-2));
            File.SetLastWriteTimeUtc(recentOwned, fixedTime.UtcDateTime.AddMinutes(-5));
            File.SetLastWriteTimeUtc(foreignSimilar, fixedTime.UtcDateTime.AddDays(-2));

            var writer = new CaptureFileWriter(
                new PngCaptureEncoder(),
                new CapturePathProvider(directory),
                new FixedTimeProvider(fixedTime));

            var result = await writer.SavePngAsync(CreateFrame());

            Assert.True(File.Exists(result.FilePath));
            Assert.False(File.Exists(staleOwned));
            Assert.True(File.Exists(recentOwned));
            Assert.True(File.Exists(foreignSimilar));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SavePngAsync_LeavesStaleOwnedTemporaryFileWhenItIsLocked()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var fixedTime = new DateTimeOffset(2026, 9, 19, 0, 0, 0, TimeSpan.Zero);
            var staleOwned = Path.Combine(
                directory,
                $".SNAPVERE_2026-09-17_120000.png.{Guid.NewGuid():N}.tmp");
            File.WriteAllText(staleOwned, "locked");
            File.SetLastWriteTimeUtc(staleOwned, fixedTime.UtcDateTime.AddDays(-2));

            await using var lockStream = new FileStream(
                staleOwned,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None);

            var writer = new CaptureFileWriter(
                new PngCaptureEncoder(),
                new CapturePathProvider(directory),
                new FixedTimeProvider(fixedTime));

            var result = await writer.SavePngAsync(CreateFrame());

            Assert.True(File.Exists(result.FilePath));
            Assert.True(File.Exists(staleOwned));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SavePngAsync_InvalidDirectoryTargetReturnsTypedPersistenceFailure()
    {
        var root = CreateTemporaryDirectory();
        var fileTarget = Path.Combine(root, "capture-target");
        File.WriteAllText(fileTarget, "not a directory");

        try
        {
            var writer = new CaptureFileWriter(
                new PngCaptureEncoder(),
                new CapturePathProvider(fileTarget),
                TimeProvider.System);

            var exception = await Assert.ThrowsAsync<CapturePersistenceException>(
                () => writer.SavePngAsync(CreateFrame()));

            Assert.Equal(CapturePersistenceFailureKind.WriteFailed, exception.Kind);
            Assert.IsType<IOException>(exception.InnerException);
            Assert.True(File.Exists(fileTarget));
            Assert.Equal("not a directory", File.ReadAllText(fileTarget));
            Assert.Single(Directory.EnumerateFileSystemEntries(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SavePngAsync_CancelledTokenDoesNotPublishCapture()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var writer = new CaptureFileWriter(
                new PngCaptureEncoder(),
                new CapturePathProvider(directory),
                TimeProvider.System);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => writer.SavePngAsync(CreateFrame(), cancellation.Token));

            Assert.Empty(Directory.EnumerateFileSystemEntries(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static CaptureFrame CreateFrame()
        => new(
            new PixelSize(2, 1),
            8,
            new byte[]
            {
                0, 0, 255, 255,
                0, 255, 0, 255
            },
            DateTimeOffset.UnixEpoch,
            "capture-writer-test");

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "SNAPVERE-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => utcNow.ToUniversalTime();
    }
}
