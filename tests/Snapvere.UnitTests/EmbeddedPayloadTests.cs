using System.IO.Compression;
using Snapvere.Packaging;

namespace Snapvere.UnitTests;

public sealed class EmbeddedPayloadTests
{
    [Fact]
    public void ExtractZipSafely_ExtractsFilesInsideDestination()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-payload-{Guid.NewGuid():N}");

        try
        {
            using var payload = BuildZip(("app/Snapvere.exe", "binary"), ("data/readme.txt", "hello"));

            EmbeddedPayload.ExtractZipSafely(payload, directory);

            Assert.Equal("binary", File.ReadAllText(Path.Combine(directory, "app", "Snapvere.exe")));
            Assert.Equal("hello", File.ReadAllText(Path.Combine(directory, "data", "readme.txt")));
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }

    [Fact]
    public void ExtractZipSafely_RejectsParentDirectoryTraversal()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-payload-{Guid.NewGuid():N}");
        var escapedPath = Path.Combine(Path.GetDirectoryName(directory)!, $"escaped-{Guid.NewGuid():N}.txt");

        try
        {
            using var payload = BuildZip(($"../{Path.GetFileName(escapedPath)}", "blocked"));

            Assert.Throws<InvalidDataException>(() => EmbeddedPayload.ExtractZipSafely(payload, directory));
            Assert.False(File.Exists(escapedPath));
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
            if (File.Exists(escapedPath))
            {
                File.Delete(escapedPath);
            }
        }
    }

    private static MemoryStream BuildZip(params (string Name, string Content)[] entries)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
