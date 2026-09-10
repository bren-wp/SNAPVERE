using Snapvere.Packaging;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Snapvere.Portable;

internal static class Program
{
    private const string PayloadResourceName = "Snapvere.Payload.zip";
    private const string AppExecutableName = "Snapvere.exe";
    private const string StartupProbeEnvironmentVariable = "SNAPVERE_STARTUP_PROBE";
    private const string TrayStartupProbeEnvironmentVariable = "SNAPVERE_TRAY_STARTUP_PROBE";
    private const string RegionOverlayProbeEnvironmentVariable = "SNAPVERE_REGION_OVERLAY_PROBE";
    private const string WindowOverlayProbeEnvironmentVariable = "SNAPVERE_WINDOW_OVERLAY_PROBE";
    private const string PortableLauncherEnvironmentVariable = "SNAPVERE_PORTABLE_LAUNCHER_PATH";

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
            var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
            using var launchMutex = new Mutex(
                initiallyOwned: false,
                $@"Local\Brendigo.SNAPVERE.Portable.{version}.{architecture}");

            var ownsMutex = false;
            try
            {
                try
                {
                    ownsMutex = launchMutex.WaitOne(TimeSpan.FromSeconds(60));
                }
                catch (AbandonedMutexException)
                {
                    ownsMutex = true;
                }

                if (!ownsMutex)
                {
                    throw new TimeoutException("Another SNAPVERE Portable launch is still preparing its application files.");
                }

                var cacheRoot = Path.Combine(
                    Path.GetTempPath(),
                    "SNAPVERE",
                    "Portable",
                    $"{version}-{architecture}");
                var readyMarker = Path.Combine(cacheRoot, ".ready");
                var executable = Path.Combine(cacheRoot, AppExecutableName);

                if (!File.Exists(readyMarker) || !File.Exists(executable))
                {
                    EmbeddedPayload.DeleteDirectoryBestEffort(cacheRoot);
                    Directory.CreateDirectory(cacheRoot);

                    using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResourceName)
                        ?? throw new InvalidOperationException("The SNAPVERE application payload is missing from this portable package.");
                    EmbeddedPayload.ExtractZipSafely(payload, cacheRoot);

                    if (!File.Exists(executable))
                    {
                        throw new InvalidDataException("The portable package does not contain Snapvere.exe.");
                    }

                    File.WriteAllText(readyMarker, $"SNAPVERE {version} {architecture}");
                }

                CleanupOldCaches(Path.GetDirectoryName(cacheRoot)!, cacheRoot);
                LaunchApplication(executable, cacheRoot, args);
            }
            finally
            {
                if (ownsMutex)
                {
                    launchMutex.ReleaseMutex();
                }
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"SNAPVERE Portable could not start.\r\n\r\n{exception.Message}\r\n\r\nStartup log (when available):\r\n{GetStartupLogPath()}",
                "SNAPVERE Portable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.ExitCode = 1;
        }
    }

    private static void LaunchApplication(string executable, string workingDirectory, IReadOnlyList<string> args)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false
        };

        foreach (var argument in args)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var portableLauncher = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(portableLauncher))
        {
            startInfo.Environment[PortableLauncherEnvironmentVariable] = Path.GetFullPath(portableLauncher);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows could not create the SNAPVERE process.");

        if (IsValidationProbeRequested(args))
        {
            if (!process.WaitForExit(20_000))
            {
                TryTerminate(process);
                throw new TimeoutException("SNAPVERE did not complete its validation probe within 20 seconds.");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"SNAPVERE validation probe failed with exit code {process.ExitCode}.");
            }

            Environment.ExitCode = 0;
            return;
        }

        if (process.WaitForExit(2500))
        {
            throw new InvalidOperationException(
                $"SNAPVERE exited during startup with code {process.ExitCode}. " +
                "See the startup log path below for details.");
        }
    }

    private static bool IsValidationProbeRequested(IReadOnlyList<string> args)
    {
        if (string.Equals(
                Environment.GetEnvironmentVariable(StartupProbeEnvironmentVariable),
                "1",
                StringComparison.Ordinal) ||
            string.Equals(
                Environment.GetEnvironmentVariable(TrayStartupProbeEnvironmentVariable),
                "1",
                StringComparison.Ordinal) ||
            string.Equals(
                Environment.GetEnvironmentVariable(RegionOverlayProbeEnvironmentVariable),
                "1",
                StringComparison.Ordinal) ||
            string.Equals(
                Environment.GetEnvironmentVariable(WindowOverlayProbeEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            return true;
        }

        return args.Any(argument =>
            string.Equals(argument, "--startup-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--tray-startup-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--region-overlay-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--window-overlay-probe", StringComparison.OrdinalIgnoreCase));
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                _ = process.WaitForExit(5000);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }

    private static string GetStartupLogPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SNAPVERE",
            "Logs",
            "startup.log");

    private static void CleanupOldCaches(string portableRoot, string currentCache)
    {
        if (!Directory.Exists(portableRoot))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(portableRoot))
        {
            if (string.Equals(Path.GetFullPath(directory), Path.GetFullPath(currentCache), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }
}
