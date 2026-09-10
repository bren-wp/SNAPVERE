using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class SnapvereLocalizationTests
{
    private static readonly string[] SecondaryUiKeys =
    [
        "PreferencesStoredLocally",
        "ImplementedSettingsOnly",
        "Startup",
        "StartupDescription",
        "Capture",
        "CursorDescription",
        "CurrentLanguageDescription",
        "Refresh",
        "PreferenceReadFailed",
        "StartupEnabled",
        "StartupDisabled",
        "StartupChangeFailed",
        "CursorEnabled",
        "CursorDisabled",
        "CursorSaveFailed",
        "OneLocalCapture",
        "LocalCaptureCount",
        "LocalHistoryUnavailable",
        "NoCapturesYet",
        "EmptyHistoryHelp",
        "OpenCaptureNamed",
        "CaptureUnavailable",
        "OpenCaptureFailed",
        "OpenCaptureFolderFailed",
        "Off",
        "On",
        "LanguagePickerIntro",
        "LanguageSaveFailed",
        "AboutLocalFirstTitle",
        "AboutCaptureDescription",
        "AboutPrivacyDescription",
        "CommercialSoftware"
    ];

    [Fact]
    public void SupportedLanguages_ContainsEnglishCroatianAndAllPublishedChoices()
    {
        Assert.Equal(28, SnapvereLocalization.SupportedLanguages.Count);
        Assert.Contains(SnapvereLocalization.SupportedLanguages, language => language.Code == "en");
        Assert.Contains(SnapvereLocalization.SupportedLanguages, language => language.Code == "hr");
        Assert.Contains(SnapvereLocalization.SupportedLanguages, language => language.Code == "zh-CN");
    }

    [Theory]
    [InlineData(null, "en")]
    [InlineData("", "en")]
    [InlineData("EN-us", "en")]
    [InlineData("hr-HR", "hr")]
    [InlineData("x32", "en")]
    [InlineData("zh-CN", "zh-CN")]
    public void NormalizeLanguageCode_ReturnsSupportedStableCode(string? input, string expected)
        => Assert.Equal(expected, SnapvereLocalization.NormalizeLanguageCode(input));

    [Theory]
    [InlineData("CaptureRegion", "Snimi područje")]
    [InlineData("RegionCaptureTitle", "SNAPVERE — Snimanje područja")]
    [InlineData("WindowCaptureTitle", "SNAPVERE — Snimanje prozora")]
    [InlineData("RegionHint", "Povuci za odabir · označi izravno · Enter spremi · Esc odustani")]
    [InlineData("WindowHint", "Pokaži na prozor · klikni za snimanje · Esc za odustajanje")]
    [InlineData("SavingRegion", "Spremanje odabranog područja…")]
    [InlineData("RegionAccessDenied", "Windows je odbio pristup mapi snimki ili međuspremniku.")]
    [InlineData("RegionIoFailure", "Odabrano područje je snimljeno, ali PNG datoteku ili prijenos u međuspremnik nije bilo moguće dovršiti.")]
    [InlineData("RegionInvalidSelection", "Odabrano područje više nije valjano. Ponovno odaberite područje.")]
    [InlineData("RegionCaptureFailed", "SNAPVERE nije mogao dovršiti snimanje područja. Pritisnite Esc i pokušajte ponovno.")]
    public void Croatian_CaptureSurfaceText_IsTranslated(string key, string expected)
        => Assert.Equal(expected, SnapvereLocalization.T(key, "hr"));

    [Fact]
    public void SecondaryUiKeys_HaveDedicatedCroatianTranslations()
    {
        foreach (var key in SecondaryUiKeys)
        {
            var english = SnapvereLocalization.T(key, "en");
            var croatian = SnapvereLocalization.T(key, "hr");

            Assert.False(string.IsNullOrWhiteSpace(english));
            Assert.False(string.IsNullOrWhiteSpace(croatian));
            Assert.NotEqual(english, croatian);
        }
    }

    [Fact]
    public void SecondaryUiKeys_FallBackToCanonicalEnglish_WhenTranslationIsMissing()
    {
        foreach (var key in SecondaryUiKeys)
        {
            Assert.Equal(
                SnapvereLocalization.T(key, "en"),
                SnapvereLocalization.T(key, "de"));
        }
    }

    [Theory]
    [InlineData("OpenCaptureNamed", "sample.png", "Otvori sample.png")]
    [InlineData("LocalCaptureCount", 7, "Lokalne snimke: 7")]
    [InlineData("LanguageSaveFailed", "disk", "Postavku jezika nije moguće spremiti: disk")]
    public void CroatianFormattedSecondaryUiCopy_PreservesFormatArguments(
        string key,
        object argument,
        string expected)
    {
        var template = SnapvereLocalization.T(key, "hr");
        Assert.Equal(expected, string.Format(template, argument));
    }

    [Theory]
    [InlineData("WindowHint", "Point to a window · click to capture · Esc to cancel")]
    [InlineData("RegionMoveHelp", "Move or resize selection")]
    [InlineData("ResizeBottomRight", "Resize bottom right")]
    [InlineData("RegionCaptureFailed", "SNAPVERE could not complete the region capture. Press Esc and try again.")]
    public void MissingCaptureSurfaceTranslation_FallsBackToEnglish(string key, string expected)
        => Assert.Equal(expected, SnapvereLocalization.T(key, "ja"));

    [Fact]
    public void MissingTranslation_FallsBackToEnglish()
        => Assert.Equal("Start SNAPVERE with Windows", SnapvereLocalization.T("StartWithWindows", "ja"));

    [Fact]
    public void UnknownKey_ReturnsKeyInsteadOfThrowing()
        => Assert.Equal("MissingKey", SnapvereLocalization.T("MissingKey", "hr"));
}
