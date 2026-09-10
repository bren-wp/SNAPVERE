using Snapvere.Application.Capture;

namespace Snapvere.UnitTests;

public sealed class CaptureRelocationServiceTests
{
    [Fact]
    public async Task RelocateAsync_MovesCompletedCaptureToSelectedDestinationAndOverwritesExistingFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-relocate-{Guid.NewGuid():N}");
        var sourceDirectory = Path.Combine(root, "source");
        var destinationDirectory = Path.Combine(root, "chosen");
        var sourcePath = Path.Combine(sourceDirectory, "SNAPVERE_2026-09-11_001500.png");
        var destinationPath = Path.Combine(destinationDirectory, "final-name.png");
        var expectedBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3, 4 };
        var capturedAt = new DateTimeOffset(2026, 9, 11, 0, 15, 0, TimeSpan.FromHours(2));

        try
        {
            Directory.CreateDirectory(sourceDirectory);
            Directory.CreateDirectory(destinationDirectory);
            await File.WriteAllBytesAsync(sourcePath, expectedBytes);
            await File.WriteAllBytesAsync(destinationPath, new byte[] { 9, 9, 9 });

            var capture = new CaptureSaveResult(sourcePath, 1920, 1080, capturedAt);
            var result = await new CaptureRelocationService().RelocateAsync(capture, destinationPath);

            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(expectedBytes, await File.ReadAllBytesAsync(destinationPath));
            Assert.Equal(Path.GetFullPath(destinationPath), result.FilePath);
            Assert.Equal(1920, result.Width);
            Assert.Equal(1080, result.Height);
            Assert.Equal(capturedAt, result.CapturedAt);
            Assert.Empty(Directory.EnumerateFiles(destinationDirectory, "*.tmp", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RelocateAsync_SupportsLongValidDestinationNameWithoutExpandingStagingName()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-relocate-long-{Guid.NewGuid():N}");
        var sourceDirectory = Path.Combine(root, "source");
        var destinationDirectory = Path.Combine(root, "chosen");
        var sourcePath = Path.Combine(sourceDirectory, "SNAPVERE_source.png");
        var destinationPath = Path.Combine(destinationDirectory, $"{new string('a', 230)}.png");
        var expectedBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 7, 8, 9 };

        try
        {
            Directory.CreateDirectory(sourceDirectory);
            Directory.CreateDirectory(destinationDirectory);
            await File.WriteAllBytesAsync(sourcePath, expectedBytes);

            var capture = new CaptureSaveResult(sourcePath, 640, 360, DateTimeOffset.UtcNow);
            var result = await new CaptureRelocationService().RelocateAsync(capture, destinationPath);

            Assert.Equal(Path.GetFullPath(destinationPath), result.FilePath);
            Assert.Equal(expectedBytes, await File.ReadAllBytesAsync(destinationPath));
            Assert.Empty(Directory.EnumerateFiles(destinationDirectory, ".snapvere-*.tmp", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RelocateAsync_RejectsNonPngDestinationWithoutTouchingCompletedCapture()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-relocate-invalid-{Guid.NewGuid():N}");
        var sourcePath = Path.Combine(root, "SNAPVERE_2026-09-11_001500.png");
        var invalidDestination = Path.Combine(root, "capture.jpg");

        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllBytesAsync(sourcePath, new byte[] { 1, 2, 3, 4 });
            var capture = new CaptureSaveResult(sourcePath, 4, 4, DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<ArgumentException>(
                () => new CaptureRelocationService().RelocateAsync(capture, invalidDestination));

            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(invalidDestination));
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RelocateAsync_CancellationKeepsOriginalAndRemovesStagingFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-relocate-cancel-{Guid.NewGuid():N}");
        var sourcePath = Path.Combine(root, "SNAPVERE_source.png");
        var destinationPath = Path.Combine(root, "chosen", "cancelled.png");

        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllBytesAsync(sourcePath, new byte[] { 1, 2, 3, 4 });
            var capture = new CaptureSaveResult(sourcePath, 1, 1, DateTimeOffset.UtcNow);
            using var cancellation = new CancellationTokenSource();
            await cancellation.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => new CaptureRelocationService().RelocateAsync(
                    capture,
                    destinationPath,
                    cancellation.Token));

            Assert.True(File.Exists(sourcePath));
            Assert.False(File.Exists(destinationPath));
            var destinationDirectory = Path.GetDirectoryName(destinationPath)!;
            if (Directory.Exists(destinationDirectory))
            {
                Assert.Empty(Directory.EnumerateFiles(destinationDirectory, ".snapvere-*.tmp", SearchOption.TopDirectoryOnly));
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
