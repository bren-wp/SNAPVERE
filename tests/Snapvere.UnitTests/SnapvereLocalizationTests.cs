using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class SnapvereLocalizationTests
{
    [Fact]
    public void SupportedLanguages_ContainsEnglishCroatianAndMoreThanTwentyLanguages()
    {
        Assert.True(SnapvereLocalization.SupportedLanguages.Count >= 22);
        Assert.Contains(SnapvereLocalization.SupportedLanguages, language => language.Code == "en");
        Assert.Contains(SnapvereLocalization.SupportedLanguages, language => language.Code == "hr");
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

    [Fact]
    public void Croatian_CoreAction_IsTranslated()
        => Assert.Equal("Snimi područje", SnapvereLocalization.T("CaptureRegion", "hr"));

    [Fact]
    public void MissingTranslation_FallsBackToEnglish()
        => Assert.Equal("Start SNAPVERE with Windows", SnapvereLocalization.T("StartWithWindows", "ja"));

    [Fact]
    public void UnknownKey_ReturnsKeyInsteadOfThrowing()
        => Assert.Equal("MissingKey", SnapvereLocalization.T("MissingKey", "hr"));
}
