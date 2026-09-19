using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class TrayIconRecoveryPolicyTests
{
    [Theory]
    [InlineData(1, 250)]
    [InlineData(2, 500)]
    [InlineData(3, 1000)]
    [InlineData(4, 2000)]
    public void GetRetryDelayAfterFailure_ReturnsBoundedBackoff(
        int failedAttempt,
        int expectedMilliseconds)
    {
        var delay = TrayIconRecoveryPolicy.GetRetryDelayAfterFailure(failedAttempt);

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds), delay);
    }

    [Fact]
    public void GetRetryDelayAfterFailure_StopsAfterMaximumAttempt()
    {
        Assert.Null(
            TrayIconRecoveryPolicy.GetRetryDelayAfterFailure(
                TrayIconRecoveryPolicy.MaximumAttempts));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void GetRetryDelayAfterFailure_RejectsOutOfRangeAttempts(int failedAttempt)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TrayIconRecoveryPolicy.GetRetryDelayAfterFailure(failedAttempt));
    }
}
