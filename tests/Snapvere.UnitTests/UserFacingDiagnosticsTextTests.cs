using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class UserFacingDiagnosticsTextTests
{
    [Fact]
    public void StartupFailureMessage_DoesNotExposeFilesystemOrExceptionDetails()
    {
        var message = UserFacingDiagnosticsText.StartupFailureMessage;

        Assert.Contains("local diagnostic log", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(":\\", message, StringComparison.Ordinal);
        Assert.DoesNotContain("\\Users\\", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack trace", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", message, StringComparison.OrdinalIgnoreCase);
    }
}
