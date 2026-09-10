using System.Diagnostics;
using System.Reflection;

namespace Snapvere.Packaging;

/// <summary>
/// Shared lifecycle for the self-extracting Portable host. Cache, validation,
/// process and architecture behavior live here so the public launcher stays
/// small and deterministic.
/// </summary>
public static class EmbeddedAppLauncher
{
    public const string PortableLauncherEnvironmentVariable = "SNAPVERE_PORTABLE_LAUNCHER_PATH";

    private const string AppExecutableName = "Snapvere.exe";
    private const string LauncherToken = "Portable";
    private const string StartupProbeEnvironmentVariable = "SNAPVERE_STARTUP_PROBE";
    private const string TrayStartupProbeEnvironmentVariable = "SNAPVERE_TRAY_STARTUP_PROBE";
    private const string RegionOverlayProbeEnvironmentVariable = "SNAPVERE_REGION_OVERLAY_PROBE";
    private const string WindowOverlayProbeEnvironmentVariable = "SNAPVERE_WINDOW_OVERLAY_PROBE";
    private const string SecondaryUiProbeEnvironmentVariable = "SNAPVERE_SECONDARY_UI_PROBE";

    public static int Launch(Assembly hostAssembly, IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(hostAssembly);
        ArgumentNullException.ThrowIfNull(args);

        var version = hostAssembly.GetName().Version?.ToString(3) ?? "0.0.0";
        var architecture = UniversalPayload.ResolveCurrentArchitecture();
        var architectureToken = UniversalPayload.GetToken(architecture);

        using var launchMutex = new Mutex(
            initiallyOwned: false,
            $@"Local\Brendigo.SNAPVERE.{LauncherToken}.{version}.{architectureToken}");

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
                    "Another SNAPVERE Portable launch is still preparing its application files.");
            }

            var launcherRoot = Path.Combine(Path.GetTempPath(), "SNAPVERE", LauncherToken);
            var cacheRoot = Path.Combine(launcherRoot, $"{version}-{architectureToken}");
            var readyMarker = Path.Combine(cacheRoot, ".ready");
            var executable = Path.Combine(cacheRoot, AppExecutableName);

            if (!File.Exists(readyMarker) || !File.Exists(executable))
            {
                PrepareCacheTransactionally(hostAssembly, architecture, architectureToken, version, launcherRoot, cacheRoot);
            }

            CleanupOldCaches(launcherRoot, cacheRoot);
            LaunchApplication(executable, cacheRoot, args);
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

    private static void PrepareCacheTransactionally(
        Assembly hostAssembly,
        SnapverePayloadArchitecture architecture,
        string architectureToken,
        string version,
        string launcherRoot,
        string cacheRoot)
    {
        Directory.CreateDirectory(launcherRoot);
        var stagingRoot = Path.Combine(
            launcherRoot,
            $".stage-{version}-{architectureToken}-{Guid.NewGuid():N}");

        try
        {
            Directory.CreateDirectory(stagingRoot);
            using var payload = UniversalPayload.OpenEmbeddedPayload(hostAssembly, architecture);
            EmbeddedPayload.ExtractZipSafely(payload, stagingRoot);

            var stagedExecutable = Path.Combine(stagingRoot, AppExecutableName);
            if (!File.Exists(stagedExecutable))
            {
                throw new InvalidDataException(
                    $"The Portable package does not contain Snapvere.exe in its {architectureToken} payload.");
            }

            File.WriteAllText(
                Path.Combine(stagingRoot, ".ready"),
                $"SNAPVERE {version} {LauncherToken} {architectureToken}{Environment.NewLine}");

            if (Directory.Exists(cacheRoot))
            {
                Directory.Delete(cacheRoot, recursive: true);
            }

            Directory.Move(stagingRoot, cacheRoot);
        }
        finally
        {
            EmbeddedPayload.DeleteDirectoryBestEffort(stagingRoot);
        }
    }

    private static void LaunchApplication(
        string executable,
        string workingDirectory,
        IReadOnlyList<string> args)
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
            var fullPath = Path.GetFullPath(directory);
            if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase) ||
                Path.GetFileName(directory).StartsWith(".stage-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            EmbeddedPayload.DeleteDirectoryBestEffort(directory);
        }
    }
}
