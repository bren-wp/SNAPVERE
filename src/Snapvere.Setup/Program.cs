using System.Globalization;

namespace Snapvere.Setup;

internal static class Program
{
    private const string SetupMutexName = @"Local\Brendigo.SNAPVERE.Setup";
    private const string UiProbeArgument = "--ui-probe";
    private const string UiProbeMarkerFileName = "setup-ui-probe.ready";

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var silent = HasArgument(args, "--silent");
        var cleanupInstall = GetArgumentValue(args, "--cleanup-install");
        if (cleanupInstall is not null)
        {
            var waitPidText = GetArgumentValue(args, "--wait-pid");
            var waitPid = int.TryParse(waitPidText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPid)
                ? parsedPid
                : 0;
            var cleanupResult = InstallerEngine.CompleteDeferredUninstall(cleanupInstall, waitPid, silent);
            Environment.ExitCode = cleanupResult.ExitCode;
            return;
        }

        using var instanceMutex = new Mutex(initiallyOwned: false, SetupMutexName);
        var ownsMutex = false;
        try
        {
            try
            {
                ownsMutex = instanceMutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                ownsMutex = true;
            }

            if (!ownsMutex)
            {
                if (!silent)
                {
                    MessageBox.Show(
                        "Another SNAPVERE Setup operation is already running.",
                        "SNAPVERE Setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                Environment.ExitCode = 1618;
                return;
            }

            var uninstall = HasArgument(args, "--uninstall");
            var acceptLicense = HasArgument(args, "--accept-license");

            if (silent)
            {
                if (!uninstall && !acceptLicense)
                {
                    Environment.ExitCode = 2;
                    return;
                }

                var result = uninstall
                    ? InstallerEngine.Uninstall(silent: true)
                    : InstallerEngine.Install(
                        InstallerEngine.GetDefaultInstallDirectory(),
                        createStartMenuShortcut: true,
                        createDesktopShortcut: true,
                        silent: true,
                        progress: null);

                if (!uninstall && result.Succeeded)
                {
                    _ = SetupStartupRegistration.TrySetEnabled(
                        InstallerEngine.GetDefaultInstallDirectory(),
                        enabled: true,
                        out _);
                }

                Environment.ExitCode = result.ExitCode;
                return;
            }

            using var setupForm = new SetupForm(uninstall);
            if (HasArgument(args, UiProbeArgument))
            {
                setupForm.Shown += (_, _) =>
                {
                    var probeDirectory = Path.Combine(Path.GetTempPath(), "SNAPVERE");
                    Directory.CreateDirectory(probeDirectory);
                    File.WriteAllText(
                        Path.Combine(probeDirectory, UiProbeMarkerFileName),
                        $"SNAPVERE Setup UI READY | PID={Environment.ProcessId} | {DateTimeOffset.UtcNow:O}");
                    setupForm.BeginInvoke(setupForm.Close);
                };
            }

            Application.Run(setupForm);
        }
        finally
        {
            if (ownsMutex)
            {
                instanceMutex.ReleaseMutex();
            }
        }
    }

    private static bool HasArgument(string[] args, string name)
        => args.Any(argument => string.Equals(argument, name, StringComparison.OrdinalIgnoreCase));

    private static string? GetArgumentValue(string[] args, string name)
    {
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
