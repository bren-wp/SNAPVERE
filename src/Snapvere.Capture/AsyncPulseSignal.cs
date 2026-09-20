namespace Snapvere.Capture;

/// <summary>
/// Thread-safe one-shot generation signal for asynchronous producer/consumer
/// coordination. Pulse completes every waiter that observed the current
/// generation and immediately creates a fresh unsignaled generation.
///
/// The signal intentionally owns no disposable wait handle, which makes it
/// safe to pulse during teardown while an async waiter is still unwinding.
/// </summary>
internal sealed class AsyncPulseSignal
{
    private readonly object _sync = new();
    private TaskCompletionSource<bool> _generation = CreateGeneration();

    public Task WaitAsync()
    {
        lock (_sync)
        {
            return _generation.Task;
        }
    }

    public void Pulse()
    {
        TaskCompletionSource<bool> generation;
        lock (_sync)
        {
            generation = _generation;
            _generation = CreateGeneration();
        }

        _ = generation.TrySetResult(true);
    }

    private static TaskCompletionSource<bool> CreateGeneration()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
