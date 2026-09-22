using Snapvere.Packaging;

namespace Snapvere.UnitTests;

public sealed class PortableCachePolicyTests
{
    [Theory]
    [InlineData("0.1.12-x86")]
    [InlineData("0.1.12-x64")]
    [InlineData("0.1.12-arm64")]
    [InlineData("10.20.300-x64")]
    public void OwnedVersionCacheName_AcceptsSnapvereVersionArchitectureShape(string name)
        => Assert.True(PortableCachePolicy.IsOwnedVersionCacheDirectoryName(name));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".stage-0.1.12-x64-abc")]
    [InlineData("0.1-x64")]
    [InlineData("0.1.12.0-x64")]
    [InlineData("0.1.12-amd64")]
    [InlineData("0.1.12")]
    [InlineData("../0.1.12-x64")]
    [InlineData("custom-cache")]
    public void OwnedVersionCacheName_RejectsUnknownOrUnownedNames(string? name)
        => Assert.False(PortableCachePolicy.IsOwnedVersionCacheDirectoryName(name));
}
