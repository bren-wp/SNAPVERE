using Snapvere.Shared;

namespace Snapvere.UnitTests;

public sealed class CaptureActivityGateTests
{
    [Fact]
    public void ActiveCapture_BlocksShutdownUntilCaptureEnds()
    {
        var gate = new CaptureActivityGate();

        Assert.True(gate.TryBeginCapture());
        Assert.True(gate.IsCaptureInProgress);
        Assert.False(gate.TryBeginShutdown());

        gate.EndCapture();

        Assert.False(gate.IsCaptureInProgress);
        Assert.True(gate.TryBeginShutdown());
    }

    [Fact]
    public void ShutdownRequest_BlocksFutureCaptureStarts()
    {
        var gate = new CaptureActivityGate();

        Assert.True(gate.TryBeginShutdown());
        Assert.False(gate.TryBeginCapture());
        Assert.False(gate.IsCaptureInProgress);
    }
}
