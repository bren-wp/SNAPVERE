namespace Snapvere.Application.Capture;

public sealed class ScreenRecordingSessionGate : IDisposable
{
    private readonly object _gate = new();
    private ScreenRecordingSession? _activeSession;
    private bool _disposed;

    public bool IsActive
    {
        get
        {
            lock (_gate)
            {
                return _activeSession is not null;
            }
        }
    }

    public ScreenRecordingSession? TryBegin()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_activeSession is not null)
            {
                return null;
            }

            _activeSession = new ScreenRecordingSession();
            return _activeSession;
        }
    }

    public bool RequestStop()
    {
        lock (_gate)
        {
            if (_disposed ||
                _activeSession is null ||
                _activeSession.StopSource.IsCancellationRequested)
            {
                return false;
            }

            _activeSession.StopSource.Cancel();
            return true;
        }
    }

    public bool Complete(ScreenRecordingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        CancellationTokenSource? source = null;
        lock (_gate)
        {
            if (!ReferenceEquals(_activeSession, session))
            {
                return false;
            }

            _activeSession = null;
            source = session.StopSource;
        }

        source.Dispose();
        return true;
    }

    public void Dispose()
    {
        CancellationTokenSource? source = null;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_activeSession is not null)
            {
                source = _activeSession.StopSource;
                _activeSession = null;
            }
        }

        if (source is null)
        {
            return;
        }

        try
        {
            if (!source.IsCancellationRequested)
            {
                source.Cancel();
            }
        }
        finally
        {
            source.Dispose();
        }
    }
}

public sealed class ScreenRecordingSession
{
    private readonly CancellationToken _stopToken;

    internal ScreenRecordingSession()
    {
        _stopToken = StopSource.Token;
    }

    internal CancellationTokenSource StopSource { get; } = new();

    public CancellationToken StopToken => _stopToken;

    public bool IsStopRequested => _stopToken.IsCancellationRequested;
}
