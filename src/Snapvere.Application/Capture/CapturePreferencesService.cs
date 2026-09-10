using Snapvere.Shared;
using System.Text.Json;

namespace Snapvere.Application.Capture;

public sealed record CapturePreferences(
    bool IncludeCursorOnCapture = false,
    string LanguageCode = SnapvereLocalization.DefaultLanguageCode);

/// <summary>
/// Local-only capture and UI preferences. The settings file is intentionally
/// small, human-readable and written atomically so a process interruption
/// cannot leave a partially-written configuration behind.
/// </summary>
public sealed class CapturePreferencesService
{
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly object _gate = new();
    private readonly string _settingsPath;
    private CapturePreferences? _cached;

    public CapturePreferencesService()
        : this(GetDefaultSettingsPath())
    {
    }

    public CapturePreferencesService(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = Path.GetFullPath(settingsPath);
    }

    public CapturePreferences Current
    {
        get
        {
            lock (_gate)
            {
                return _cached ??= LoadCore();
            }
        }
    }

    public string SettingsPath => _settingsPath;

    public void SetIncludeCursorOnCapture(bool enabled)
    {
        lock (_gate)
        {
            var updated = (_cached ??= LoadCore()) with
            {
                IncludeCursorOnCapture = enabled
            };

            SaveCore(updated);
            _cached = updated;
        }
    }

    public void SetLanguageCode(string languageCode)
    {
        var normalized = SnapvereLocalization.NormalizeLanguageCode(languageCode);
        lock (_gate)
        {
            var updated = (_cached ??= LoadCore()) with
            {
                LanguageCode = normalized
            };

            SaveCore(updated);
            _cached = updated;
        }
    }

    private CapturePreferences LoadCore()
    {
        if (!File.Exists(_settingsPath))
        {
            return new CapturePreferences();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var parsed = JsonSerializer.Deserialize<CapturePreferences>(json, SerializerOptions)
                ?? new CapturePreferences();
            return parsed with
            {
                LanguageCode = SnapvereLocalization.NormalizeLanguageCode(parsed.LanguageCode)
            };
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new CapturePreferences();
        }
    }

    private void SaveCore(CapturePreferences preferences)
    {
        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException("The SNAPVERE settings path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(_settingsPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            var json = JsonSerializer.Serialize(preferences, SerializerOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static string GetDefaultSettingsPath()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("Windows did not provide the current user's local application-data directory.");
        }

        return Path.Combine(localApplicationData, "SNAPVERE", SettingsFileName);
    }
}
