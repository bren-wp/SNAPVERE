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
}
