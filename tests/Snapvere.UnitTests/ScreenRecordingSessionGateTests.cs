using Snapvere.Application.Capture;

namespace Snapvere.UnitTests;

public sealed class ScreenRecordingSessionGateTests
{
    [Fact]
    public void TryBegin_AllowsOnlyOneActiveSession()
    {
        using var gate = new ScreenRecordingSessionGate();

        var first = gate.TryBegin();
        var second = gate.TryBegin();

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.True(gate.IsActive);
        Assert.True(gate.Complete(first!));
        Assert.False(gate.IsActive);
    }

    [Fact]
    public void RequestStop_IsIdempotentAndCancelsActiveSession()
    {
        using var gate = new ScreenRecordingSessionGate();
        var session = Assert.IsType<ScreenRecordingSession>(gate.TryBegin());

        Assert.True(gate.RequestStop());
        Assert.True(session.StopToken.IsCancellationRequested);
        Assert.True(session.IsStopRequested);
        Assert.False(gate.RequestStop());
        Assert.True(gate.Complete(session));
    }

    [Fact]
    public void Complete_StaleSessionCannotClearNewRecording()
    {
        using var gate = new ScreenRecordingSessionGate();
        var first = Assert.IsType<ScreenRecordingSession>(gate.TryBegin());

        Assert.True(gate.RequestStop());
        Assert.True(gate.Complete(first));

        var second = Assert.IsType<ScreenRecordingSession>(gate.TryBegin());
        Assert.False(gate.Complete(first));
        Assert.True(gate.IsActive);
        Assert.True(gate.Complete(second));
    }

    [Fact]
    public void ConcurrentStopRequests_OnlyOneRequestOwnsCancellation()
    {
        using var gate = new ScreenRecordingSessionGate();
        var session = Assert.IsType<ScreenRecordingSession>(gate.TryBegin());
        var successfulRequests = 0;

        Parallel.For(
            0,
            32,
            _ =>
            {
                if (gate.RequestStop())
                {
                    Interlocked.Increment(ref successfulRequests);
                }
            });

        Assert.Equal(1, successfulRequests);
        Assert.True(session.StopToken.IsCancellationRequested);
        Assert.True(gate.Complete(session));
    }

    [Fact]
    public void Dispose_CancelsActiveSessionAndClearsState()
    {
        var gate = new ScreenRecordingSessionGate();
        var session = Assert.IsType<ScreenRecordingSession>(gate.TryBegin());

        gate.Dispose();

        Assert.True(session.StopToken.IsCancellationRequested);
        Assert.False(gate.IsActive);
        Assert.Null(Record.Exception(gate.Dispose));
        Assert.Throws<ObjectDisposedException>(() => gate.TryBegin());
    }
}
