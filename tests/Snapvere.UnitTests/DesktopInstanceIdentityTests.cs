using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class DesktopInstanceIdentityTests
{
    [Fact]
    public void GetMutexName_IsStableAndUserScoped()
    {
        var first = DesktopInstanceIdentity.GetMutexName();
        var second = DesktopInstanceIdentity.GetMutexName();

        Assert.Equal(first, second);
        Assert.StartsWith("Local\\SNAPVERE.Desktop.S-", first, StringComparison.Ordinal);
    }


    [Fact]
    public void GetActivationEventName_IsStableAndUserScoped()
    {
        var first = DesktopInstanceIdentity.GetActivationEventName();
        var second = DesktopInstanceIdentity.GetActivationEventName();

        Assert.Equal(first, second);
        Assert.StartsWith("Local\\SNAPVERE.Desktop.Activate.S-", first, StringComparison.Ordinal);
    }

    [Fact]
    public void TrySignalDesktopInstance_SignalsCanonicalActivationEvent()
    {
        using var activationEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            DesktopInstanceIdentity.GetActivationEventName());

        Assert.True(DesktopInstanceIdentity.TrySignalDesktopInstance());
        Assert.True(activationEvent.WaitOne(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void IsDesktopInstanceRunning_DetectsCanonicalMutex()
    {
        var name = DesktopInstanceIdentity.GetMutexName();
        using var mutex = new Mutex(initiallyOwned: true, name, out var createdNew);

        try
        {
            Assert.True(DesktopInstanceIdentity.IsDesktopInstanceRunning());
        }
        finally
        {
            if (createdNew)
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
