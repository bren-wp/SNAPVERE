using Snapvere.Shared;
using System.Text.RegularExpressions;

namespace Snapvere.UnitTests;

public sealed class SnapvereLocalizationTests
{
    private static readonly string[] SecondaryUiKeys =
    [
        "PreferencesStoredLocally",
        "Startup",
        "StartupDescription",
        "Capture",
        "Refresh",
        "StartupEnabled",
        "StartupDisabled",
        "CursorEnabled",
        "CursorDisabled",
        "OneLocalCapture",
        "LocalCaptureCount",
        "LocalHistoryUnavailable",
        "NoCapturesYet",
        "EmptyHistoryHelp",
        "OpenCaptureNamed",
        "CaptureUnavailable",
        "Off",
        "On",
        "LanguagePickerIntro",
        "AboutLocalFirstTitle",
        "AboutCaptureDescription",
        "AboutPrivacyDescription",
        "CommercialSoftware",
        "Support",
        "Privacy",
        "Terms"
    ];

    [Fact]
    public void SupportedLanguages_ContainsOnlyFullyTranslatedPublishedChoices()
    {
        Assert.Equal(["en", "hr"], SnapvereLocalization.SupportedLanguages.Select(language => language.Code).ToArray());
    }

    [Fact]
    public void EveryPublishedTranslation_IsCompleteAndPlaceholderCompatible()
    {
        foreach (var language in SnapvereLocalization.SupportedLanguages.Where(language => language.Code != SnapvereLocalization.DefaultLanguageCode))
        {
            foreach (var key in SnapvereLocalization.CanonicalKeys)
            {
                Assert.True(
                    SnapvereLocalization.HasDedicatedTranslation(key, language.Code),
                    $"Published language {language.Code} is missing canonical key {key}.");

                var englishPlaceholders = Placeholders(SnapvereLocalization.T(key, "en"));
                var translatedPlaceholders = Placeholders(SnapvereLocalization.T(key, language.Code));
                Assert.Equal(englishPlaceholders, translatedPlaceholders);
            }
        }
    }

    [Theory]
    [InlineData(null, "en")]
    [InlineData("", "en")]
    [InlineData("EN-us", "en")]
    [InlineData("hr-HR", "hr")]
    [InlineData("x32", "en")]
    [InlineData("zh-CN", "en")]
    [InlineData("de-DE", "en")]
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
    [InlineData("CaptureSaveFailedTitle", "Snimku nije moguće spremiti")]
    [InlineData("CaptureSaveAccessDenied", "Snimka je izrađena, ali Windows je odbio pristup SNAPVERE mapi snimki. Provjerite dozvole za mapu Slike i pokušajte ponovno.")]
    [InlineData("CaptureStorageFull", "Snimka je izrađena, ali na uređaju za pohranu nema dovoljno prostora. Oslobodite prostor i pokušajte ponovno.")]
    [InlineData("CaptureSaveFailed", "Snimka je izrađena, ali SNAPVERE nije mogao zapisati PNG datoteku. Provjerite mapu snimki i pokušajte ponovno.")]
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

    [Theory]
    [InlineData("LanguageSaved", "Jezik je spremljen. SNAPVERE sada koristi ovaj jezik.")]
    public void Croatian_LanguageSaved_ReflectsImmediateApplication(string key, string expected)
        => Assert.Equal(expected, SnapvereLocalization.T(key, "hr"));

    [Theory]
    [InlineData("OpenCaptureNamed", "sample.png", "Otvori sample.png")]
    [InlineData("LocalCaptureCount", 7, "Lokalne snimke: 7")]
    public void CroatianFormattedSecondaryUiCopy_PreservesFormatArguments(
        string key,
        object argument,
        string expected)
    {
        var template = SnapvereLocalization.T(key, "hr");
        Assert.Equal(expected, string.Format(template, argument));
    }

    [Theory]
    [InlineData("ja", "WindowHint", "Point to a window · click to capture · Esc to cancel")]
    [InlineData("de-DE", "StartWithWindows", "Start SNAPVERE with Windows")]
    public void UnsupportedLanguage_NormalizesToCanonicalEnglish(string language, string key, string expected)
        => Assert.Equal(expected, SnapvereLocalization.T(key, language));

    [Fact]
    public void UnknownKey_ReturnsKeyInsteadOfThrowing()
        => Assert.Equal("MissingKey", SnapvereLocalization.T("MissingKey", "hr"));

    private static string[] Placeholders(string value)
        => Regex.Matches(value, @"\{\d+(?::[^}]*)?\}")
            .Select(match => match.Value)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
}
