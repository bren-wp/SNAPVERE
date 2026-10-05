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
        ScreenRecordingSession? session;
        lock (_gate)
        {
            if (_disposed || _activeSession is null)
            {
                return false;
            }

            session = _activeSession;
        }

        // CancellationTokenSource.Cancel invokes callbacks synchronously. Never
        // call it while holding the gate lock because a cancellation callback
        // is allowed to complete the session and re-enter this gate.
        return session.TryRequestStop();
    }

    public bool Complete(ScreenRecordingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (_gate)
        {
            if (!ReferenceEquals(_activeSession, session))
            {
                return false;
            }

            _activeSession = null;
        }

        session.DisposeStopSourceWhenSafe();
        return true;
    }

    public void Dispose()
    {
        ScreenRecordingSession? session;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            session = _activeSession;
            _activeSession = null;
        }

        if (session is null)
        {
            return;
        }

        // Keep cancellation outside _gate for the same re-entrancy reason as
        // RequestStop. If another thread already owns cancellation, disposal is
        // deferred by the session until that cancellation callback stack exits.
        try
        {
            _ = session.TryRequestStop();
        }
        finally
        {
            // Cancellation callbacks are external code and may throw. Cleanup
            // must still run after TryRequestStop finishes unwinding so the
            // token source and its registrations cannot leak during shutdown.
            session.DisposeStopSourceWhenSafe();
        }
    }
}

public sealed class ScreenRecordingSession
{
    private readonly object _lifecycleGate = new();
    private readonly CancellationTokenSource _stopSource = new();
    private readonly CancellationToken _stopToken;
    private bool _stopCancellationStarted;
    private bool _stopCancellationFinished;
    private bool _disposeRequested;
    private bool _stopSourceDisposed;

    internal ScreenRecordingSession()
    {
        _stopToken = _stopSource.Token;
    }

    public CancellationToken StopToken => _stopToken;

    public bool IsStopRequested => _stopToken.IsCancellationRequested;

    internal bool TryRequestStop()
    {
        lock (_lifecycleGate)
        {
            if (_stopSourceDisposed || _stopCancellationStarted)
            {
                return false;
            }

            _stopCancellationStarted = true;
        }

        try
        {
            _stopSource.Cancel();
            return true;
        }
        finally
        {
            FinishStopCancellation();
        }
    }

    internal void DisposeStopSourceWhenSafe()
    {
        var disposeNow = false;
        lock (_lifecycleGate)
        {
            if (_stopSourceDisposed)
            {
                return;
            }

            if (_stopCancellationStarted && !_stopCancellationFinished)
            {
                _disposeRequested = true;
                return;
            }

            _stopSourceDisposed = true;
            disposeNow = true;
        }

        if (disposeNow)
        {
            _stopSource.Dispose();
        }
    }

    private void FinishStopCancellation()
    {
        var disposeNow = false;
        lock (_lifecycleGate)
        {
            _stopCancellationFinished = true;
            if (_disposeRequested && !_stopSourceDisposed)
            {
                _stopSourceDisposed = true;
                disposeNow = true;
            }
        }

        if (disposeNow)
        {
            _stopSource.Dispose();
        }
    }
}
