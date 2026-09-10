using Snapvere.Application.Capture;
using System.Text.Json;

namespace Snapvere.UnitTests;

public sealed class CapturePreferencesServiceTests
{
    [Fact]
    public void SetIncludeCursorOnCapture_PersistsAndLeavesNoTemporaryFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-settings-{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(directory, "settings.json");

        try
        {
            var service = new CapturePreferencesService(settingsPath);
            Assert.False(service.Current.IncludeCursorOnCapture);

            service.SetIncludeCursorOnCapture(true);

            var reloaded = new CapturePreferencesService(settingsPath);
            Assert.True(reloaded.Current.IncludeCursorOnCapture);
            Assert.True(File.Exists(settingsPath));
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp", SearchOption.TopDirectoryOnly));

            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            Assert.True(document.RootElement.GetProperty("IncludeCursorOnCapture").GetBoolean());
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
    public void InvalidSettings_FallsBackToSafeDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-settings-invalid-{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(directory, "settings.json");

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(settingsPath, "{ invalid json");

            var service = new CapturePreferencesService(settingsPath);

            Assert.False(service.Current.IncludeCursorOnCapture);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
