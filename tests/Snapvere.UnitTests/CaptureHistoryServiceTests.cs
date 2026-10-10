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
    public void GetRecentCaptures_IncludesScreenRecordingsButRejectsForeignExtensions()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-history-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var now = DateTime.UtcNow;
            var screenshot = CreateCapture(
                directory,
                "SNAPVERE_2026-09-19_200000.png",
                10,
                now.AddSeconds(-2));
            var recording = CreateCapture(
                directory,
                "SNAPVERE_Record_2026-09-19_200001.mp4",
                20,
                now.AddSeconds(-1));
            _ = CreateCapture(
                directory,
                "SNAPVERE_2026-09-19_200002.exe",
                30,
                now);

            var service = new CaptureHistoryService(new CapturePathProvider(directory));
            var recent = service.GetRecentCaptures(limit: 12);

            Assert.Equal(2, recent.Count);
            Assert.Contains(recent, item => item.FilePath == screenshot);
            Assert.Contains(recent, item => item.FilePath == recording);
            Assert.DoesNotContain(recent, item => item.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
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
    public void GetRecentCaptures_UsesFileNameAsDeterministicTieBreakerAtLimit()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-history-ties-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var timestamp = new DateTime(2026, 10, 10, 18, 0, 0, DateTimeKind.Utc);
            foreach (var suffix in new[] { "001", "004", "002", "003" })
            {
                _ = CreateCapture(
                    directory,
                    $"SNAPVERE_2026-10-10_180000_{suffix}.png",
                    8,
                    timestamp);
            }

            var history = new CaptureHistoryService(new CapturePathProvider(directory));
            var recent = history.GetRecentCaptures(limit: 2).Select(item => item.FileName).ToArray();

            Assert.Equal(new[]
            {
                "SNAPVERE_2026-10-10_180000_004.png",
                "SNAPVERE_2026-10-10_180000_003.png"
            }, recent);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CaptureHistoryItem_DisplaysLargeRecordingsInGbAndTb()
    {
        var timestamp = new DateTimeOffset(2026, 10, 10, 18, 0, 0, TimeSpan.Zero);
        var recording = new CaptureHistoryItem(
            "SNAPVERE_Record_2026-10-10_180000.mp4",
            "SNAPVERE_Record_2026-10-10_180000.mp4",
            timestamp,
            1536L * 1024L * 1024L);
        var archive = recording with { FileSizeBytes = 2L * 1024L * 1024L * 1024L * 1024L };

        Assert.EndsWith(" GB", recording.MetadataText, StringComparison.Ordinal);
        Assert.EndsWith(" TB", archive.MetadataText, StringComparison.Ordinal);
    }

    [Fact]
    public void IsCurrentCaptureFile_RejectsMissingExternalAndReplacedFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-history-open-{Guid.NewGuid():N}");
        var outside = Path.Combine(Path.GetTempPath(), $"snapvere-other-{Guid.NewGuid():N}.png");
        Directory.CreateDirectory(directory);
        try
        {
            var current = CreateCapture(directory, "SNAPVERE_2026-09-19_230000.png", 12, DateTime.UtcNow);
            File.WriteAllText(outside, "not inside SNAPVERE");
            var history = new CaptureHistoryService(new CapturePathProvider(directory));
            var item = Assert.Single(history.GetRecentCaptures());

            Assert.True(history.IsCurrentCaptureFile(item));
            Assert.False(history.IsCurrentCaptureFile(item with { FilePath = outside }));
            Assert.False(history.IsCurrentCaptureFile(item with { FileName = "../escape.png" }));

            File.Delete(current);
            Assert.False(history.IsCurrentCaptureFile(item));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
            File.Delete(outside);
        }
    }

    [Fact]
    public void IsCurrentCaptureFile_RejectsChangesToSizeOrTimestampSinceListing()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-history-stale-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var path = CreateCapture(
                directory,
                "SNAPVERE_2026-10-10_120000.png",
                12,
                new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc));
            var history = new CaptureHistoryService(new CapturePathProvider(directory));
            var item = Assert.Single(history.GetRecentCaptures());
            Assert.True(history.IsCurrentCaptureFile(item));

            // A replacement can keep its original timestamp but differ in size.
            File.WriteAllBytes(path, new byte[13]);
            File.SetLastWriteTimeUtc(path, item.ModifiedAt.UtcDateTime);
            Assert.False(history.IsCurrentCaptureFile(item));

            // A same-size replacement must also be rejected when its timestamp differs.
            File.WriteAllBytes(path, new byte[12]);
            File.SetLastWriteTimeUtc(path, item.ModifiedAt.UtcDateTime.AddMinutes(1));
            Assert.False(history.IsCurrentCaptureFile(item));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void GetRecentCaptures_DoesNotFollowSymlinksOutsideCaptureFolder()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-history-link-{Guid.NewGuid():N}");
        var outside = Path.Combine(Path.GetTempPath(), $"snapvere-private-{Guid.NewGuid():N}.png");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(outside, "outside capture directory");
            var link = Path.Combine(directory, "SNAPVERE_2026-09-19_222000.png");
            try
            {
                File.CreateSymbolicLink(link, outside);
            }
            catch (Exception exception) when (
                exception is UnauthorizedAccessException or IOException or NotSupportedException)
            {
                // Some Windows configurations deny non-admin symlink creation.
                return;
            }

            var real = CreateCapture(
                directory, "SNAPVERE_2026-09-19_222001.png", 12, DateTime.UtcNow);
            var recent = new CaptureHistoryService(new CapturePathProvider(directory)).GetRecentCaptures();

            Assert.Single(recent);
            Assert.Equal(real, recent[0].FilePath);
            Assert.DoesNotContain(recent, item => item.FilePath == link);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
            File.Delete(outside);
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
