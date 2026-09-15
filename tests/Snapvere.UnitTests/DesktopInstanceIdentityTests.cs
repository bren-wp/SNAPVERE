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
