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

    [Fact]
    public async Task CaptureAndShutdownStartingTogether_HaveExactlyOneWinner()
    {
        for (var iteration = 0; iteration < 256; iteration++)
        {
            var gate = new CaptureActivityGate();
            using var start = new Barrier(3);

            var capture = Task.Run(() =>
            {
                start.SignalAndWait();
                return gate.TryBeginCapture();
            });
            var shutdown = Task.Run(() =>
            {
                start.SignalAndWait();
                return gate.TryBeginShutdown();
            });

            start.SignalAndWait();
            var captureWon = await capture;
            var shutdownWon = await shutdown;

            Assert.NotEqual(captureWon, shutdownWon);

            if (captureWon)
            {
                gate.EndCapture();
                Assert.True(gate.TryBeginShutdown());
            }
            else
            {
                Assert.False(gate.TryBeginCapture());
            }
        }
    }
}
