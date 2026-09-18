using Snapvere.Packaging;

namespace Snapvere.UnitTests;

public sealed class InstallSafetyPolicyTests
{
    [Fact]
    public void HasExactMarkerHeader_AcceptsExactHeader()
    {
        var marker = "SNAPVERE-INSTALLATION-V1\r\n0.1.6\r\nx64\r\n";

        Assert.True(InstallSafetyPolicy.HasExactMarkerHeader(
            marker,
            "SNAPVERE-INSTALLATION-V1"));
    }

    [Fact]
    public void HasExactMarkerHeader_RejectsPrefixOnlyMatch()
    {
        var marker = "SNAPVERE-INSTALLATION-V1-TAMPERED\r\n0.1.6\r\nx64\r\n";

        Assert.False(InstallSafetyPolicy.HasExactMarkerHeader(
            marker,
            "SNAPVERE-INSTALLATION-V1"));
    }

    [Fact]
    public void NormalizeProductDirectory_AppendsProductFolderOnce()
    {
        var parent = Path.Combine(Path.GetTempPath(), "snapvere-install-policy-parent");

        var normalized = InstallSafetyPolicy.NormalizeProductDirectory(parent, "SNAPVERE");

        Assert.Equal(
            Path.Combine(Path.GetFullPath(parent), "SNAPVERE"),
            normalized);
    }

    [Fact]
    public void NormalizeProductDirectory_DoesNotDuplicateExistingProductLeaf()
    {
        var selected = Path.Combine(Path.GetTempPath(), "snapvere-install-policy-parent", "snapvere");

        var normalized = InstallSafetyPolicy.NormalizeProductDirectory(selected, "SNAPVERE");

        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(selected)), normalized);
    }

    [Fact]
    public void EnsureExistingDirectoryChainHasNoReparsePoints_AllowsNormalDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-install-policy-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            InstallSafetyPolicy.EnsureExistingDirectoryChainHasNoReparsePoints(root);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
