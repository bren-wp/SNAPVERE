using System.Diagnostics;
using System.Reflection;

namespace Snapvere.Packaging;

public enum SnapvereLauncherMode
{
    Portable,
    Demo
}

/// <summary>
/// Shared lifecycle for self-extracting Portable and Demo hosts. Keeping this
/// in one place prevents cache, probe, process and architecture behavior from
/// drifting between public executables.
/// </summary>
public static class EmbeddedAppLauncher
{
    public const string PortableLauncherEnvironmentVariable = "SNAPVERE_PORTABLE_LAUNCHER_PATH";
    public const string DemoModeEnvironmentVariable = "SNAPVERE_DEMO_MODE";

    private const string AppExecutableName = "Snapvere.exe";
    private const string StartupProbeEnvironmentVariable = "SNAPVERE_STARTUP_PROBE";
    private const string TrayStartupProbeEnvironmentVariable = "SNAPVERE_TRAY_STARTUP_PROBE";
    private const string RegionOverlayProbeEnvironmentVariable = "SNAPVERE_REGION_OVERLAY_PROBE";
    private const string WindowOverlayProbeEnvironmentVariable = "SNAPVERE_WINDOW_OVERLAY_PROBE";
    private const string SecondaryUiProbeEnvironmentVariable = "SNAPVERE_SECONDARY_UI_PROBE";

    public static int Launch(
        Assembly hostAssembly,
        IReadOnlyList<string> args,
        SnapvereLauncherMode mode)
    {
        ArgumentNullException.ThrowIfNull(hostAssembly);
        ArgumentNullException.ThrowIfNull(args);

        var version = hostAssembly.GetName().Version?.ToString(3) ?? "0.0.0";
        var architecture = UniversalPayload.ResolveCurrentArchitecture();
        var architectureToken = UniversalPayload.GetToken(architecture);
        var modeToken = mode.ToString();

        using var launchMutex = new Mutex(
            initiallyOwned: false,
            $@"Local\Brendigo.SNAPVERE.{modeToken}.{version}.{architectureToken}");

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
                throw new TimeoutException(
                    $"Another SNAPVERE {modeToken} launch is still preparing its application files.");
            }

            var cacheRoot = Path.Combine(
                Path.GetTempPath(),
                "SNAPVERE",
                modeToken,
                $"{version}-{architectureToken}");
            var readyMarker = Path.Combine(cacheRoot, ".ready");
            var executable = Path.Combine(cacheRoot, AppExecutableName);

            if (!File.Exists(readyMarker) || !File.Exists(executable))
            {
                EmbeddedPayload.DeleteDirectoryBestEffort(cacheRoot);
                Directory.CreateDirectory(cacheRoot);

                using var payload = UniversalPayload.OpenEmbeddedPayload(hostAssembly, architecture);
                EmbeddedPayload.ExtractZipSafely(payload, cacheRoot);

                if (!File.Exists(executable))
                {
                    throw new InvalidDataException(
                        $"The {modeToken} package does not contain Snapvere.exe in its {architectureToken} payload.");
                }

                File.WriteAllText(
                    readyMarker,
                    $"SNAPVERE {version} {modeToken} {architectureToken}{Environment.NewLine}");
            }

            CleanupOldCaches(Path.GetDirectoryName(cacheRoot)!, cacheRoot);
            LaunchApplication(executable, cacheRoot, args, mode);
            return 0;
        }
        finally
        {
            if (ownsMutex)
            {
                launchMutex.ReleaseMutex();
            }
        }
    }

    public static string GetStartupLogPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SNAPVERE",
            "Logs",
            "startup.log");

    private static void LaunchApplication(
        string executable,
        string workingDirectory,
        IReadOnlyList<string> args,
        SnapvereLauncherMode mode)
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

        var launcherPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(launcherPath))
        {
            startInfo.Environment[PortableLauncherEnvironmentVariable] = Path.GetFullPath(launcherPath);
        }

        if (mode == SnapvereLauncherMode.Demo)
        {
            startInfo.Environment[DemoModeEnvironmentVariable] = "1";
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows could not create the SNAPVERE process.");

        if (IsValidationProbeRequested(args))
        {
            if (!process.WaitForExit(25_000))
            {
                TryTerminate(process);
                throw new TimeoutException("SNAPVERE did not complete its validation probe within 25 seconds.");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"SNAPVERE validation probe failed with exit code {process.ExitCode}.");
            }

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
        if (IsEnvironmentProbeEnabled(StartupProbeEnvironmentVariable) ||
            IsEnvironmentProbeEnabled(TrayStartupProbeEnvironmentVariable) ||
            IsEnvironmentProbeEnabled(RegionOverlayProbeEnvironmentVariable) ||
            IsEnvironmentProbeEnabled(WindowOverlayProbeEnvironmentVariable) ||
            IsEnvironmentProbeEnabled(SecondaryUiProbeEnvironmentVariable))
        {
            return true;
        }

        return args.Any(argument =>
            string.Equals(argument, "--startup-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--tray-startup-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--region-overlay-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--window-overlay-probe", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--secondary-ui-probe", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsEnvironmentProbeEnabled(string variable)
        => string.Equals(
            Environment.GetEnvironmentVariable(variable),
            "1",
            StringComparison.Ordinal);

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

    private static void CleanupOldCaches(string launcherRoot, string currentCache)
    {
        if (!Directory.Exists(launcherRoot))
        {
            return;
        }

        var currentFullPath = Path.GetFullPath(currentCache);
        foreach (var directory in Directory.EnumerateDirectories(launcherRoot))
        {
            if (string.Equals(Path.GetFullPath(directory), currentFullPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }
}
