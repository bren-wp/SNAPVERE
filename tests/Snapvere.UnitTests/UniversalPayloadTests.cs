using System.Reflection;
using System.Runtime.InteropServices;
using Snapvere.Packaging;

namespace Snapvere.UnitTests;

public sealed class UniversalPayloadTests
{
    [Theory]
    [InlineData(Architecture.X86, SnapverePayloadArchitecture.X86)]
    [InlineData(Architecture.X64, SnapverePayloadArchitecture.X64)]
    [InlineData(Architecture.Arm64, SnapverePayloadArchitecture.Arm64)]
    public void ResolveArchitecture_UsesNativeArchitectureByDefault(
        Architecture operatingSystemArchitecture,
        SnapverePayloadArchitecture expected)
    {
        Assert.Equal(
            expected,
            UniversalPayload.ResolveArchitecture(operatingSystemArchitecture, requestedArchitecture: null));
    }

    [Theory]
    [InlineData("x32")]
    [InlineData("32")]
    [InlineData("x86")]
    [InlineData("win-x86")]
    public void ResolveArchitecture_AcceptsCommon32BitAliases(string requested)
    {
        Assert.Equal(
            SnapverePayloadArchitecture.X86,
            UniversalPayload.ResolveArchitecture(Architecture.X64, requested));
    }

    [Theory]
    [InlineData("x64")]
    [InlineData("amd64")]
    [InlineData("64")]
    [InlineData("win-x64")]
    public void ResolveArchitecture_AcceptsCommon64BitAliases(string requested)
    {
        Assert.Equal(
            SnapverePayloadArchitecture.X64,
            UniversalPayload.ResolveArchitecture(Architecture.X64, requested));
    }

    [Fact]
    public void ResolveArchitecture_AllowsX86CompatibilityPayloadOnArm64()
    {
        Assert.Equal(
            SnapverePayloadArchitecture.X86,
            UniversalPayload.ResolveArchitecture(Architecture.Arm64, "x86"));
    }

    [Theory]
    [InlineData(Architecture.X86, "x64")]
    [InlineData(Architecture.X86, "arm64")]
    [InlineData(Architecture.X64, "arm64")]
    [InlineData(Architecture.Arm64, "x64")]
    public void ResolveArchitecture_RejectsIncompatibleOverrides(
        Architecture operatingSystemArchitecture,
        string requested)
    {
        Assert.Throws<PlatformNotSupportedException>(() =>
            UniversalPayload.ResolveArchitecture(operatingSystemArchitecture, requested));
    }

    [Fact]
    public void ResolveArchitecture_RejectsUnknownOverride()
    {
        Assert.Throws<ArgumentException>(() =>
            UniversalPayload.ResolveArchitecture(Architecture.X64, "mips64"));
    }

    [Theory]
    [InlineData(SnapverePayloadArchitecture.X86, "Snapvere.Payload.x86.zip")]
    [InlineData(SnapverePayloadArchitecture.X64, "Snapvere.Payload.x64.zip")]
    [InlineData(SnapverePayloadArchitecture.Arm64, "Snapvere.Payload.arm64.zip")]
    public void GetResourceName_IsDeterministic(
        SnapverePayloadArchitecture architecture,
        string expected)
    {
        Assert.Equal(expected, UniversalPayload.GetResourceName(architecture));
    }

    [Theory]
    [InlineData(SnapverePayloadArchitecture.X86)]
    [InlineData(SnapverePayloadArchitecture.X64)]
    [InlineData(SnapverePayloadArchitecture.Arm64)]
    public void OpenEmbeddedPayload_RejectsMissingArchitectureResource(
        SnapverePayloadArchitecture architecture)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            using var _ = UniversalPayload.OpenEmbeddedPayload(
                Assembly.GetExecutingAssembly(),
                architecture);
        });

        Assert.Contains(
            UniversalPayload.GetToken(architecture),
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }
}
