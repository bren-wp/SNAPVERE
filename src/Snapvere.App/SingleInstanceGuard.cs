using System.Runtime.CompilerServices;
using Snapvere.Shared;

namespace Snapvere.App;

internal static class SingleInstanceGuard
{
    private static Mutex? _lifetimeMutex;

    [ModuleInitializer]
    internal static void Initialize()
    {
        if (IsAutomationProbeLaunch())
        {
            return;
        }

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
            mutex.Dispose();
            Environment.Exit(0);
            return;
        }

        // The named mutex is intentionally kept for the complete process lifetime.
        // Windows releases ownership automatically if the process exits or crashes.
        _lifetimeMutex = mutex;
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
