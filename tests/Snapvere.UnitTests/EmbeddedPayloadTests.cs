using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

    [Fact]
    public void ExtractZipSafely_RejectsDuplicateFileDestinations()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-payload-{Guid.NewGuid():N}");

        try
        {
            using var payload = BuildZip(("data/readme.txt", "first"), ("data/readme.txt", "second"));

            Assert.Throws<InvalidDataException>(() => EmbeddedPayload.ExtractZipSafely(payload, directory));
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }

    [Fact]
    public void ExtractZipSafely_AllowsLongValidFilenameWithoutOverflowingStagingComponent()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-payload-{Guid.NewGuid():N}");
        var fileName = $"{new string('a', 220)}.txt";

        try
        {
            using var payload = BuildZip(($"data/{fileName}", "long-name"));

            EmbeddedPayload.ExtractZipSafely(payload, directory);

            Assert.Equal("long-name", File.ReadAllText(Path.Combine(directory, "data", fileName)));
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(directory, "data"), ".snapvere-*.tmp"));
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }

    [Fact]
    public void IsExtractedPayloadIntact_AcceptsExactPayloadAndAllowedReadyMarker()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-payload-{Guid.NewGuid():N}");

        try
        {
            using (var payload = BuildZip(("Snapvere.exe", "binary"), ("data/readme.txt", "hello")))
            {
                EmbeddedPayload.ExtractZipSafely(payload, directory);
            }

            File.WriteAllText(Path.Combine(directory, ".ready"), "ready");

            using var manifest = BuildIntegrityManifest(("Snapvere.exe", "binary"), ("data/readme.txt", "hello"));
            Assert.True(EmbeddedPayload.IsExtractedPayloadIntact(manifest, directory, [".ready"]));
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }

    [Fact]
    public void IsExtractedPayloadIntact_RejectsTamperedCachedFileWithSameLength()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-payload-{Guid.NewGuid():N}");

        try
        {
            using (var payload = BuildZip(("Snapvere.exe", "original"), ("data/readme.txt", "hello")))
            {
                EmbeddedPayload.ExtractZipSafely(payload, directory);
            }

            File.WriteAllText(Path.Combine(directory, "Snapvere.exe"), "tampered");

            using var manifest = BuildIntegrityManifest(("Snapvere.exe", "original"), ("data/readme.txt", "hello"));
            Assert.False(EmbeddedPayload.IsExtractedPayloadIntact(manifest, directory));
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }

    [Fact]
    public void IsExtractedPayloadIntact_RejectsMissingCachedFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-payload-{Guid.NewGuid():N}");

        try
        {
            using (var payload = BuildZip(("Snapvere.exe", "binary"), ("data/readme.txt", "hello")))
            {
                EmbeddedPayload.ExtractZipSafely(payload, directory);
            }

            File.Delete(Path.Combine(directory, "data", "readme.txt"));

            using var manifest = BuildIntegrityManifest(("Snapvere.exe", "binary"), ("data/readme.txt", "hello"));
            Assert.False(EmbeddedPayload.IsExtractedPayloadIntact(manifest, directory));
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }

    [Fact]
    public void IsExtractedPayloadIntact_RejectsUnexpectedCachedFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-payload-{Guid.NewGuid():N}");

        try
        {
            using (var payload = BuildZip(("Snapvere.exe", "binary"), ("data/readme.txt", "hello")))
            {
                EmbeddedPayload.ExtractZipSafely(payload, directory);
            }

            File.WriteAllText(Path.Combine(directory, "unexpected.dll"), "injected");

            using var manifest = BuildIntegrityManifest(("Snapvere.exe", "binary"), ("data/readme.txt", "hello"));
            Assert.False(EmbeddedPayload.IsExtractedPayloadIntact(manifest, directory));
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }

    [Fact]
    public void IsExtractedPayloadIntact_RejectsAllowedExtraPathOutsideRoot()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-payload-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(directory);
            using var manifest = BuildIntegrityManifest(("Snapvere.exe", "binary"));

            Assert.Throws<ArgumentException>(() =>
                EmbeddedPayload.IsExtractedPayloadIntact(manifest, directory, ["../outside.txt"]));
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }

    [Fact]
    public void IsExtractedPayloadIntact_RejectsManifestPathTraversal()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-payload-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(directory);
            using var manifest = BuildIntegrityManifest(("../outside.txt", "blocked"));

            Assert.Throws<InvalidDataException>(() =>
                EmbeddedPayload.IsExtractedPayloadIntact(manifest, directory));
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
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

    private static MemoryStream BuildIntegrityManifest(params (string Name, string Content)[] entries)
    {
        var files = entries
            .Select(entry =>
            {
                var bytes = Encoding.UTF8.GetBytes(entry.Content);
                return new
                {
                    Path = entry.Name,
                    Length = (long)bytes.Length,
                    Sha256 = Convert.ToHexString(SHA256.HashData(bytes))
                };
            })
            .ToArray();

        var json = JsonSerializer.Serialize(new
        {
            FormatVersion = 1,
            Files = files
        });
        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }
}
