using Microsoft.Win32;
using System.Security;

namespace Snapvere.App.Services;

/// <summary>
/// Per-user Windows startup registration. Normal SNAPVERE launch is tray-first,
/// so the Run entry uses the same stable executable path without a special
/// startup mode. Portable builds receive the stable launcher path from the
/// portable host so a Run entry never points into the temporary extraction cache.
/// Demo mode never creates or mutates Windows startup registration.
/// </summary>
public sealed class StartupRegistrationService
{
    public const string PortableLauncherEnvironmentVariable = "SNAPVERE_PORTABLE_LAUNCHER_PATH";
    public const string DemoModeEnvironmentVariable = "SNAPVERE_DEMO_MODE";
    public const string RunValueName = "SNAPVERE";
    public const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _launchPath;
    private readonly bool _demoMode;

    public StartupRegistrationService()
    {
        _demoMode = string.Equals(
            Environment.GetEnvironmentVariable(DemoModeEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

        var portableLauncher = Environment.GetEnvironmentVariable(PortableLauncherEnvironmentVariable);
        var candidate = string.IsNullOrWhiteSpace(portableLauncher)
            ? Environment.ProcessPath
            : portableLauncher;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidOperationException("Windows did not provide the SNAPVERE executable path for startup registration.");
        }

        _launchPath = Path.GetFullPath(candidate);
    }

    public string LaunchPath => _launchPath;

    public bool CanConfigureStartup => !_demoMode;

    public bool IsEnabled()
    {
        if (_demoMode)
        {
            return false;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: false);
        var registered = key?.GetValue(RunValueName) as string;
        return string.Equals(registered, BuildLaunchCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        if (_demoMode)
        {
            if (enabled)
            {
                throw new SecurityException("SNAPVERE Demo does not register itself to start with Windows.");
            }
            return;
        }

        if (enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunRegistryPath, writable: true)
                ?? throw new InvalidOperationException("Windows could not open the current user's startup registry key.");
            key.SetValue(RunValueName, BuildLaunchCommand(), RegistryValueKind.String);
            return;
        }

        using var existing = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true);
        existing?.DeleteValue(RunValueName, throwOnMissingValue: false);
    }

    private string BuildLaunchCommand()
        => $"\"{_launchPath}\"";
}
