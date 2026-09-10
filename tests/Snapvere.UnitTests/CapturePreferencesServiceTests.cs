using Snapvere.Application.Capture;
using Snapvere.Shared;
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
            Assert.Equal("en", service.Current.LanguageCode);

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
    public void SetLanguageCode_NormalizesPersistsUpdatesProcessStateAndNotifiesOnce()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-language-{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(directory, "settings.json");
        var previousLanguage = SnapvereLanguageState.CurrentLanguageCode;
        var notifications = 0;
        EventHandler handler = (_, _) => notifications++;

        try
        {
            SnapvereLanguageState.SetCurrentLanguage("en");
            SnapvereLanguageState.CurrentLanguageChanged += handler;

            var service = new CapturePreferencesService(settingsPath);
            service.SetLanguageCode("hr-HR");

            Assert.Equal("hr", service.Current.LanguageCode);
            Assert.Equal("hr", SnapvereLanguageState.CurrentLanguageCode);

            var reloaded = new CapturePreferencesService(settingsPath);
            Assert.Equal("hr", reloaded.Current.LanguageCode);
            Assert.Equal("hr", SnapvereLanguageState.CurrentLanguageCode);
            Assert.Equal(1, notifications);

            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            Assert.Equal("hr", document.RootElement.GetProperty("LanguageCode").GetString());
        }
        finally
        {
            SnapvereLanguageState.CurrentLanguageChanged -= handler;
            SnapvereLanguageState.SetCurrentLanguage(previousLanguage);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void InvalidSettings_FallsBackToSafeEnglishDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-settings-invalid-{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(directory, "settings.json");
        var previousLanguage = SnapvereLanguageState.CurrentLanguageCode;

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(settingsPath, "{ invalid json");

            var service = new CapturePreferencesService(settingsPath);

            Assert.False(service.Current.IncludeCursorOnCapture);
            Assert.Equal("en", service.Current.LanguageCode);
            Assert.Equal("en", SnapvereLanguageState.CurrentLanguageCode);
        }
        finally
        {
            SnapvereLanguageState.SetCurrentLanguage(previousLanguage);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void UnsupportedLanguageInSettings_IsNormalizedToEnglish()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"snapvere-settings-language-invalid-{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(directory, "settings.json");
        var previousLanguage = SnapvereLanguageState.CurrentLanguageCode;

        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(settingsPath, "{\"IncludeCursorOnCapture\":true,\"LanguageCode\":\"xx-YY\"}");

            var service = new CapturePreferencesService(settingsPath);

            Assert.True(service.Current.IncludeCursorOnCapture);
            Assert.Equal("en", service.Current.LanguageCode);
        }
        finally
        {
            SnapvereLanguageState.SetCurrentLanguage(previousLanguage);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
