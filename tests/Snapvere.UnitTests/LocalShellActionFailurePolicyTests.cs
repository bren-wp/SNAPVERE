using System.ComponentModel;
using System.Diagnostics;
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
    public void Open_UsesShellExecutionForRequestedTarget()
    {
        ProcessStartInfo? observed = null;

        LocalShellAction.Open(
            @"C:\captures\sample.png",
            startInfo =>
            {
                observed = startInfo;
                return new Process();
            });

        Assert.NotNull(observed);
        Assert.Equal(@"C:\captures\sample.png", observed.FileName);
        Assert.True(observed.UseShellExecute);
    }

    [Fact]
    public void Open_RejectsMissingShellHandler()
    {
        Assert.Throws<InvalidOperationException>(
            () => LocalShellAction.Open(
                @"C:\captures\sample.png",
                _ => null));
    }

    [Fact]
    public void Open_RejectsBlankTarget()
    {
        Assert.Throws<ArgumentException>(
            () => LocalShellAction.Open(" "));
    }
}
