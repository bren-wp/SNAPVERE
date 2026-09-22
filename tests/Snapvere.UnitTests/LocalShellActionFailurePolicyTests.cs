using System.ComponentModel;
using System.Security;
using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class LocalShellActionFailurePolicyTests
{
    public static TheoryData<Exception> ExpectedFailures => new()
    {
        new IOException("I/O failure"),
        new UnauthorizedAccessException("Access denied"),
        new SecurityException("Policy blocked"),
        new InvalidOperationException("Shell handler unavailable"),
        new Win32Exception(2, "Windows shell failure")
    };

    [Theory]
    [MemberData(nameof(ExpectedFailures))]
    public void IsExpected_ReturnsTrueForContainedShellFailures(Exception exception)
    {
        Assert.True(LocalShellActionFailurePolicy.IsExpected(exception));
    }

    [Fact]
    public void IsExpected_ReturnsFalseForUnexpectedProgrammingFailure()
    {
        Assert.False(LocalShellActionFailurePolicy.IsExpected(
            new ArgumentOutOfRangeException("value")));
    }

    [Fact]
    public void IsExpected_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => LocalShellActionFailurePolicy.IsExpected(null!));
    }

    [Fact]
    public void EnsureStarted_AllowsSuccessfulShellLaunch()
    {
        LocalShellActionFailurePolicy.EnsureStarted(true, "test action");
    }

    [Fact]
    public void EnsureStarted_RejectsSilentNullShellLaunch()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => LocalShellActionFailurePolicy.EnsureStarted(false, "capture folder"));

        Assert.Contains("capture folder", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EnsureStarted_RejectsMissingOperationName(string operation)
    {
        Assert.Throws<ArgumentException>(
            () => LocalShellActionFailurePolicy.EnsureStarted(true, operation));
    }
}
