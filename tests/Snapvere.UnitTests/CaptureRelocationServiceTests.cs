using Snapvere.Application.Capture;

namespace Snapvere.UnitTests;

public sealed class CaptureRelocationServiceTests
{
    [Fact]
    public void Relocate_MovesCompletedCaptureToSelectedDestinationAndOverwritesExistingFile()
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
            File.WriteAllBytes(sourcePath, expectedBytes);
            File.WriteAllBytes(destinationPath, new byte[] { 9, 9, 9 });

            var capture = new CaptureSaveResult(sourcePath, 1920, 1080, capturedAt);
            var result = new CaptureRelocationService().Relocate(capture, destinationPath);

            Assert.False(File.Exists(sourcePath));
            Assert.True(File.Exists(destinationPath));
            Assert.Equal(expectedBytes, File.ReadAllBytes(destinationPath));
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
    public void Relocate_RejectsNonPngDestinationWithoutTouchingCompletedCapture()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-relocate-invalid-{Guid.NewGuid():N}");
        var sourcePath = Path.Combine(root, "SNAPVERE_2026-09-11_001500.png");
        var invalidDestination = Path.Combine(root, "capture.jpg");

        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllBytes(sourcePath, new byte[] { 1, 2, 3, 4 });
            var capture = new CaptureSaveResult(sourcePath, 4, 4, DateTimeOffset.UtcNow);

            Assert.Throws<ArgumentException>(
                () => new CaptureRelocationService().Relocate(capture, invalidDestination));

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
}
