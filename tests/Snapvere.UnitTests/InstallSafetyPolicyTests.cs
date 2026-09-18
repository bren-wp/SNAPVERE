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
    [Fact]
    public void EnsureInstallTargetIsOwnedOrEmpty_AllowsMissingDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-missing-target-{Guid.NewGuid():N}");

        InstallSafetyPolicy.EnsureInstallTargetIsOwnedOrEmpty(
            root,
            ".snapvere-installation",
            "SNAPVERE-INSTALLATION-V1",
            "Snapvere.exe",
            "SNAPVERE-Setup.exe");
    }

    [Fact]
    public void EnsureInstallTargetIsOwnedOrEmpty_AllowsEmptyDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-empty-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            InstallSafetyPolicy.EnsureInstallTargetIsOwnedOrEmpty(
                root,
                ".snapvere-installation",
                "SNAPVERE-INSTALLATION-V1",
                "Snapvere.exe",
                "SNAPVERE-Setup.exe");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EnsureInstallTargetIsOwnedOrEmpty_AllowsValidatedExistingInstall()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-owned-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(root, ".snapvere-installation"),
                "SNAPVERE-INSTALLATION-V1\r\n0.1.6\r\nx64\r\n");
            File.WriteAllText(Path.Combine(root, "Snapvere.exe"), "probe");

            InstallSafetyPolicy.EnsureInstallTargetIsOwnedOrEmpty(
                root,
                ".snapvere-installation",
                "SNAPVERE-INSTALLATION-V1",
                "Snapvere.exe",
                "SNAPVERE-Setup.exe");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EnsureInstallTargetIsOwnedOrEmpty_RejectsNonEmptyUnownedDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-unowned-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "personal-file.txt"), "keep me");

            var exception = Assert.Throws<InvalidOperationException>(() =>
                InstallSafetyPolicy.EnsureInstallTargetIsOwnedOrEmpty(
                    root,
                    ".snapvere-installation",
                    "SNAPVERE-INSTALLATION-V1",
                    "Snapvere.exe",
                    "SNAPVERE-Setup.exe"));

            Assert.StartsWith("SNAPVERE cannot be installed", exception.Message, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(root, "personal-file.txt")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EnsureInstallTargetIsOwnedOrEmpty_RejectsInvalidMarker()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-invalid-marker-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(root, ".snapvere-installation"),
                "SNAPVERE-INSTALLATION-V1-TAMPERED\r\n0.1.6\r\nx64\r\n");
            File.WriteAllText(Path.Combine(root, "Snapvere.exe"), "probe");

            Assert.Throws<InvalidOperationException>(() =>
                InstallSafetyPolicy.EnsureInstallTargetIsOwnedOrEmpty(
                    root,
                    ".snapvere-installation",
                    "SNAPVERE-INSTALLATION-V1",
                    "Snapvere.exe",
                    "SNAPVERE-Setup.exe"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EnsureInstallTargetIsOwnedOrEmpty_RejectsMarkerWithoutOwnedApplicationFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"snapvere-marker-only-target-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(
                Path.Combine(root, ".snapvere-installation"),
                "SNAPVERE-INSTALLATION-V1\r\n0.1.6\r\nx64\r\n");

            Assert.Throws<InvalidOperationException>(() =>
                InstallSafetyPolicy.EnsureInstallTargetIsOwnedOrEmpty(
                    root,
                    ".snapvere-installation",
                    "SNAPVERE-INSTALLATION-V1",
                    "Snapvere.exe",
                    "SNAPVERE-Setup.exe"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

}
