namespace Snapvere.Shared;

/// <summary>
/// Bounded retry schedule used when Explorer recreates the notification area.
/// The first tray re-registration attempt is immediate; this policy controls
/// only the delayed retries after a failed attempt.
/// </summary>
public static class TrayIconRecoveryPolicy
{
    public const int MaximumAttempts = 5;

    public static TimeSpan? GetRetryDelayAfterFailure(int failedAttempt)
    {
        if (failedAttempt < 1 || failedAttempt > MaximumAttempts)
        {
            throw new ArgumentOutOfRangeException(nameof(failedAttempt));
        }

        if (failedAttempt == MaximumAttempts)
        {
            return null;
        }

        return failedAttempt switch
        {
            1 => TimeSpan.FromMilliseconds(250),
            2 => TimeSpan.FromMilliseconds(500),
            3 => TimeSpan.FromSeconds(1),
            4 => TimeSpan.FromSeconds(2),
            _ => null
        };
    }
}
