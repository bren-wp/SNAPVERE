using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class SnapvereLocalizationTests
{
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
