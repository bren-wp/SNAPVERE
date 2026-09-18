using System.ComponentModel;
using System.Security;
using Snapvere.Packaging;

namespace Snapvere.UnitTests;

public sealed class PortableStartupDiagnosticsTests
{
    private const string SecretDetail = @"C:\Users\private-user\Secret Project\payload.zip";

    [Theory]
    [InlineData("invalid-data", "could not be validated")]
    [InlineData("unauthorized", "blocked")]
    [InlineData("security", "blocked")]
    [InlineData("io", "could not prepare")]
    [InlineData("timeout", "did not finish startup in time")]
    [InlineData("platform", "architecture is not supported")]
    [InlineData("win32", "Windows could not start")]
    [InlineData("generic", "could not start")]
    public void GetUserFacingFailureMessage_IsSanitizedAndActionable(
        string failureKind,
        string expectedGuidance)
    {
        var exception = CreateException(failureKind);

        var message = PortableStartupDiagnostics.GetUserFacingFailureMessage(exception);

        Assert.Contains(expectedGuidance, message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            @"%LOCALAPPDATA%\SNAPVERE\Logs\startup.log",
            message,
            StringComparison.Ordinal);
        Assert.DoesNotContain(SecretDetail, message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-user", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetUserFacingFailureMessage_DoesNotExposeInnerExceptionDetails()
    {
        var exception = new InvalidOperationException(
            "outer safe-looking failure",
            new IOException($"Internal payload path: {SecretDetail}"));

        var message = PortableStartupDiagnostics.GetUserFacingFailureMessage(exception);

        Assert.DoesNotContain("outer safe-looking failure", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Internal payload path", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(SecretDetail, message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetStartupLogPath_UsesLocalSnapvereLogsFolder()
    {
        var path = PortableStartupDiagnostics.GetStartupLogPath();

        Assert.EndsWith(
            Path.Combine("SNAPVERE", "Logs", "startup.log"),
            path,
            StringComparison.OrdinalIgnoreCase);
    }

    private static Exception CreateException(string failureKind)
        => failureKind switch
        {
            "invalid-data" => new InvalidDataException($"Corrupt package: {SecretDetail}"),
            "unauthorized" => new UnauthorizedAccessException($"Denied: {SecretDetail}"),
            "security" => new SecurityException($"Policy blocked: {SecretDetail}"),
            "io" => new IOException($"I/O failed: {SecretDetail}"),
            "timeout" => new TimeoutException($"Timed out at: {SecretDetail}"),
            "platform" => new PlatformNotSupportedException($"Unsupported at: {SecretDetail}"),
            "win32" => new Win32Exception(5, $"Launch failed at: {SecretDetail}"),
            _ => new InvalidOperationException($"Unexpected failure at: {SecretDetail}")
        };
}
