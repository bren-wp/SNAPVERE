namespace Snapvere.Shared;

/// <summary>
/// Process-local language state. This is deliberately just one normalized
/// string reference: no timers, file watchers, services or background work.
/// </summary>
public static class SnapvereLanguageState
{
    private static string _currentLanguageCode = SnapvereLocalization.DefaultLanguageCode;

    public static string CurrentLanguageCode
        => Volatile.Read(ref _currentLanguageCode);

    public static void SetCurrentLanguage(string? languageCode)
        => Volatile.Write(
            ref _currentLanguageCode,
            SnapvereLocalization.NormalizeLanguageCode(languageCode));
}
