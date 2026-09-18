using System.Runtime.CompilerServices;
using Snapvere.Shared;

namespace Snapvere.App;

internal static class SingleInstanceGuard
{
    private static readonly object ActivationGate = new();

    private static Mutex? _lifetimeMutex;
    private static EventWaitHandle? _activationEvent;
    private static Action? _secondLaunchHandler;
    private static bool _pendingSecondLaunch;

    [ModuleInitializer]
    internal static void Initialize()
    {
        if (IsAutomationProbeLaunch())
        {
            return;
        }

        var activationEvent = new EventWaitHandle(
            false,
            EventResetMode.AutoReset,
            DesktopInstanceIdentity.GetActivationEventName());

        var mutex = new Mutex(
            initiallyOwned: true,
            DesktopInstanceIdentity.GetMutexName(),
            out var createdNew);
        var ownsMutex = createdNew;

        if (!createdNew)
        {
            try
            {
                ownsMutex = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // WaitOne acquires an abandoned mutex before throwing. Treat it
                // as available so a crashed prior process cannot block startup.
                ownsMutex = true;
            }
        }

        if (!ownsMutex)
        {
            // The duplicate launch does not create a second UI/tray process.
            // Instead it wakes the existing per-user instance so the user's
            // launch action produces visible feedback.
            _ = activationEvent.Set();
            activationEvent.Dispose();
            mutex.Dispose();
            Environment.Exit(0);
            return;
        }

        // The named mutex and activation event intentionally live for the full
        // process lifetime. Windows releases them automatically on process exit.
        _lifetimeMutex = mutex;
        _activationEvent = activationEvent;

        var activationThread = new Thread(() => WaitForSecondLaunchSignals(activationEvent))
        {
            IsBackground = true,
            Name = "SNAPVERE Activation"
        };
        activationThread.Start();
    }

    internal static void RegisterSecondLaunchHandler(Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var invokePending = false;
        lock (ActivationGate)
        {
            _secondLaunchHandler = handler;
            if (_pendingSecondLaunch)
            {
                _pendingSecondLaunch = false;
                invokePending = true;
            }
        }

        if (invokePending)
        {
            TryInvokeSecondLaunchHandler(handler);
        }
    }

    private static void WaitForSecondLaunchSignals(EventWaitHandle activationEvent)
    {
        while (true)
        {
            try
            {
                activationEvent.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            Action? handler;
            lock (ActivationGate)
            {
                handler = _secondLaunchHandler;
                if (handler is null)
                {
                    _pendingSecondLaunch = true;
                    continue;
                }
            }

            TryInvokeSecondLaunchHandler(handler);
        }
    }

    private static void TryInvokeSecondLaunchHandler(Action handler)
    {
        try
        {
            handler();
        }
        catch
        {
            // Activation feedback must never terminate the singleton listener.
            // The normal application diagnostics path handles UI work after the
            // request reaches the dispatcher.
        }
    }

    private static bool IsAutomationProbeLaunch()
    {
        if (IsEnvironmentProbeEnabled("SNAPVERE_STARTUP_PROBE") ||
            IsEnvironmentProbeEnabled("SNAPVERE_TRAY_STARTUP_PROBE") ||
            IsEnvironmentProbeEnabled("SNAPVERE_REGION_OVERLAY_PROBE") ||
            IsEnvironmentProbeEnabled("SNAPVERE_WINDOW_OVERLAY_PROBE") ||
            IsEnvironmentProbeEnabled("SNAPVERE_SECONDARY_UI_PROBE"))
        {
            return true;
        }

        return Environment.GetCommandLineArgs().Any(argument =>
            string.Equals(argument, "--startup-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--tray-startup-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--region-overlay-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--window-overlay-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--secondary-ui-probe", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsEnvironmentProbeEnabled(string name)
        => string.Equals(Environment.GetEnvironmentVariable(name), "1", StringComparison.Ordinal);
}
