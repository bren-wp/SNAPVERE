using Snapvere.Capture;

namespace Snapvere.UnitTests;

public sealed class AsyncPulseSignalTests
{
    [Fact]
    public async Task Pulse_WakesEveryWaiterFromCurrentGeneration()
    {
        var signal = new AsyncPulseSignal();
        var first = signal.WaitAsync();
        var second = signal.WaitAsync();

        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);

        signal.Pulse();

        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Pulse_CreatesFreshUnsignaledGenerationForFutureWaiters()
    {
        var signal = new AsyncPulseSignal();
        var beforePulse = signal.WaitAsync();

        signal.Pulse();
        await beforePulse.WaitAsync(TimeSpan.FromSeconds(2));

        var afterPulse = signal.WaitAsync();
        Assert.False(afterPulse.IsCompleted);

        signal.Pulse();
        await afterPulse.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task RepeatedPulseWaitCycles_DoNotLoseCurrentGenerationWakeup()
    {
        var signal = new AsyncPulseSignal();

        for (var iteration = 0; iteration < 256; iteration++)
        {
            var waiter = signal.WaitAsync();
            signal.Pulse();
            await waiter.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }
}
