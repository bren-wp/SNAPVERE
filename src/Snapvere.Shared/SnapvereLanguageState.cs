namespace Snapvere.Shared;

/// <summary>
/// Process-local language state. This is deliberately just one normalized
/// string reference plus an in-process change notification: no timers, file
/// watchers, services or background work.
/// </summary>
public static class SnapvereLanguageState
{
    private static string _currentLanguageCode = SnapvereLocalization.DefaultLanguageCode;

    public static event EventHandler? CurrentLanguageChanged;

    public static string CurrentLanguageCode
        => Volatile.Read(ref _currentLanguageCode);

    public static void SetCurrentLanguage(string? languageCode)
    {
        var normalized = SnapvereLocalization.NormalizeLanguageCode(languageCode);
        var previous = Interlocked.Exchange(ref _currentLanguageCode, normalized);
        if (!string.Equals(previous, normalized, StringComparison.Ordinal))
        {
            CurrentLanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
