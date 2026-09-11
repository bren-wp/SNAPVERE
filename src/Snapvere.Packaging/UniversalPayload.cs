using System.Reflection;
using System.Runtime.InteropServices;

namespace Snapvere.Packaging;

public enum SnapverePayloadArchitecture
{
    X86,
    X64,
    Arm64
}

/// <summary>
/// Resolves the native SNAPVERE application payload compatible with the
/// current Windows installation. Public hosts are x86-compatible executables
/// that embed explicit x86, x64 and ARM64 application resources.
/// </summary>
public static class UniversalPayload
{
    public const string ArchitectureOverrideEnvironmentVariable = "SNAPVERE_PAYLOAD_ARCH";

    public static SnapverePayloadArchitecture ResolveCurrentArchitecture()
        => ResolveArchitecture(
            RuntimeInformation.OSArchitecture,
            Environment.GetEnvironmentVariable(ArchitectureOverrideEnvironmentVariable));

    public static SnapverePayloadArchitecture ResolveArchitecture(
        Architecture operatingSystemArchitecture,
        string? requestedArchitecture)
    {
        var nativeArchitecture = operatingSystemArchitecture switch
        {
            Architecture.X86 => SnapverePayloadArchitecture.X86,
            Architecture.X64 => SnapverePayloadArchitecture.X64,
            Architecture.Arm64 => SnapverePayloadArchitecture.Arm64,
            _ => throw new PlatformNotSupportedException(
                $"SNAPVERE does not provide a Windows payload for {operatingSystemArchitecture}.")
        };

        if (string.IsNullOrWhiteSpace(requestedArchitecture))
        {
            return nativeArchitecture;
        }

        var requested = requestedArchitecture.Trim().ToLowerInvariant() switch
        {
            "x86" or "win-x86" or "x32" or "32" => SnapverePayloadArchitecture.X86,
            "x64" or "win-x64" or "amd64" or "64" => SnapverePayloadArchitecture.X64,
            "arm64" or "win-arm64" => SnapverePayloadArchitecture.Arm64,
            _ => throw new ArgumentException(
                $"Unknown SNAPVERE payload architecture override '{requestedArchitecture}'.",
                nameof(requestedArchitecture))
        };

        if (!IsCompatible(nativeArchitecture, requested))
        {
            throw new PlatformNotSupportedException(
                $"The {GetToken(requested)} SNAPVERE payload cannot run on {GetToken(nativeArchitecture)} Windows.");
        }

        return requested;
    }

    public static Stream OpenEmbeddedPayload(
        Assembly hostAssembly,
        out SnapverePayloadArchitecture architecture)
    {
        ArgumentNullException.ThrowIfNull(hostAssembly);
        architecture = ResolveCurrentArchitecture();
        return OpenEmbeddedPayload(hostAssembly, architecture);
    }

    public static Stream OpenEmbeddedPayload(
        Assembly hostAssembly,
        SnapverePayloadArchitecture architecture)
    {
        ArgumentNullException.ThrowIfNull(hostAssembly);

        var resourceName = GetResourceName(architecture);
        return hostAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"The SNAPVERE {GetToken(architecture)} application payload is missing from this universal package.");
    }

    public static Stream OpenEmbeddedIntegrityManifest(
        Assembly hostAssembly,
        SnapverePayloadArchitecture architecture)
    {
        ArgumentNullException.ThrowIfNull(hostAssembly);

        var resourceName = GetIntegrityManifestResourceName(architecture);
        return hostAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"The SNAPVERE {GetToken(architecture)} payload integrity manifest is missing from this universal package.");
    }

    public static string GetResourceName(SnapverePayloadArchitecture architecture)
        => $"Snapvere.Payload.{GetToken(architecture)}.zip";

    public static string GetIntegrityManifestResourceName(SnapverePayloadArchitecture architecture)
        => $"Snapvere.Payload.{GetToken(architecture)}.integrity.json";

    public static string GetToken(SnapverePayloadArchitecture architecture)
        => architecture switch
        {
            SnapverePayloadArchitecture.X86 => "x86",
            SnapverePayloadArchitecture.X64 => "x64",
            SnapverePayloadArchitecture.Arm64 => "arm64",
            _ => throw new ArgumentOutOfRangeException(nameof(architecture), architecture, null)
        };

    private static bool IsCompatible(
        SnapverePayloadArchitecture operatingSystemArchitecture,
        SnapverePayloadArchitecture requestedArchitecture)
        => operatingSystemArchitecture switch
        {
            SnapverePayloadArchitecture.X86 => requestedArchitecture == SnapverePayloadArchitecture.X86,
            SnapverePayloadArchitecture.X64 => requestedArchitecture is SnapverePayloadArchitecture.X86 or SnapverePayloadArchitecture.X64,
            SnapverePayloadArchitecture.Arm64 => requestedArchitecture is SnapverePayloadArchitecture.X86 or SnapverePayloadArchitecture.Arm64,
            _ => false
        };
}
