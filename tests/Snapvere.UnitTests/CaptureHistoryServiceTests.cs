using Snapvere.Application.Capture;

namespace Snapvere.UnitTests;

public sealed class CaptureHistoryServiceTests
{
    [Fact]
    public void GetRecentCaptures_ReturnsNewestFilesFirstAndHonorsLimit()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-history-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var oldest = CreateCapture(directory, "SNAPVERE_2026-09-09_100000.png", 10, DateTime.UtcNow.AddMinutes(-3));
            var middle = CreateCapture(directory, "SNAPVERE_2026-09-09_100100.png", 20, DateTime.UtcNow.AddMinutes(-2));
            var newest = CreateCapture(directory, "SNAPVERE_2026-09-09_100200.png", 30, DateTime.UtcNow.AddMinutes(-1));
            File.WriteAllText(Path.Combine(directory, "not-a-capture.txt"), "ignore");

            var service = new CaptureHistoryService(new CapturePathProvider(directory));
            var recent = service.GetRecentCaptures(limit: 2);

            Assert.Collection(
                recent,
                item =>
                {
                    Assert.Equal(Path.GetFileName(newest), item.FileName);
                    Assert.Equal(30, item.FileSizeBytes);
                },
                item =>
                {
                    Assert.Equal(Path.GetFileName(middle), item.FileName);
                    Assert.Equal(20, item.FileSizeBytes);
                });

            Assert.DoesNotContain(recent, item => item.FilePath == oldest);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void GetRecentCaptures_BoundsLargeDirectoriesToRequestedTopN()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-history-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var baseline = DateTime.UtcNow.AddHours(-1);
            for (var index = 0; index < 100; index++)
            {
                _ = CreateCapture(
                    directory,
                    $"SNAPVERE_2026-09-09_12{index:000}.png",
                    1,
                    baseline.AddSeconds(index));
            }

            var service = new CaptureHistoryService(new CapturePathProvider(directory));
            var recent = service.GetRecentCaptures(limit: 5);

            Assert.Equal(5, recent.Count);
            Assert.True(recent.Zip(recent.Skip(1), (left, right) => left.ModifiedAt >= right.ModifiedAt).All(value => value));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DeleteCapture_RejectsPathOutsideCaptureDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-history-{Guid.NewGuid():N}");
        var outside = Path.Combine(Path.GetTempPath(), $"SNAPVERE-outside-{Guid.NewGuid():N}.png");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(outside, [1, 2, 3]);

        try
        {
            var service = new CaptureHistoryService(new CapturePathProvider(directory));

            Assert.Throws<InvalidOperationException>(() => service.DeleteCapture(outside));
            Assert.True(File.Exists(outside));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
            File.Delete(outside);
        }
    }

    [Fact]
    public void DeleteCapture_RejectsNonSnapverePngInsideCaptureDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-history-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var unrelatedPng = Path.Combine(directory, "holiday.png");
            File.WriteAllBytes(unrelatedPng, [1, 2, 3]);
            var service = new CaptureHistoryService(new CapturePathProvider(directory));

            Assert.Throws<InvalidOperationException>(() => service.DeleteCapture(unrelatedPng));
            Assert.True(File.Exists(unrelatedPng));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DeleteCapture_DeletesValidatedLocalCapture()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-history-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var capture = CreateCapture(
                directory,
                "SNAPVERE_2026-09-09_110000.png",
                4,
                DateTime.UtcNow);
            var service = new CaptureHistoryService(new CapturePathProvider(directory));

            Assert.True(service.DeleteCapture(capture));
            Assert.False(File.Exists(capture));
            Assert.False(service.DeleteCapture(capture));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateCapture(
        string directory,
        string fileName,
        int byteCount,
        DateTime modifiedUtc)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllBytes(path, Enumerable.Range(0, byteCount).Select(value => (byte)value).ToArray());
        File.SetLastWriteTimeUtc(path, modifiedUtc);
        return path;
    }
}
