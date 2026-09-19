namespace Snapvere.Shared;

public sealed class CaptureActivityGate
{
    private readonly object _sync = new();
    private bool _captureInProgress;
    private bool _shutdownRequested;

    public bool TryBeginCapture()
    {
        lock (_sync)
        {
            if (_captureInProgress || _shutdownRequested)
            {
                return false;
            }

            _captureInProgress = true;
            return true;
        }
    }

    public void EndCapture()
    {
        lock (_sync)
        {
            _captureInProgress = false;
        }
    }

    public bool TryBeginShutdown()
    {
        lock (_sync)
        {
            if (_captureInProgress)
            {
                return false;
            }

            _shutdownRequested = true;
            return true;
        }
    }

    public bool IsCaptureInProgress
    {
        get
        {
            lock (_sync)
            {
                return _captureInProgress;
            }
        }
    }
}
