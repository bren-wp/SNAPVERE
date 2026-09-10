using Microsoft.Win32;

namespace Snapvere.App.Services;

/// <summary>
/// Per-user Windows startup registration. SNAPVERE is tray-first on normal
/// launch, so the Run entry points directly to the stable executable path.
/// Portable builds receive the stable launcher path from the portable host so
/// startup never points into the temporary extraction cache.
/// </summary>
public sealed class StartupRegistrationService
{
    public const string PortableLauncherEnvironmentVariable = "SNAPVERE_PORTABLE_LAUNCHER_PATH";
    public const string RunValueName = "SNAPVERE";
    public const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _launchPath;

    public StartupRegistrationService()
    {
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

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: false);
        var registered = key?.GetValue(RunValueName) as string;
        return string.Equals(registered, BuildLaunchCommand(), StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunRegistryPath, writable: true)
                ?? throw new InvalidOperationException("Windows could not open the current user's startup registry key.");
            key.SetValue(RunValueName, BuildLaunchCommand(), RegistryValueKind.String);
            return;
        }

        using var existing = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true);
        if (existing is null)
        {
            return;
        }

        var registered = existing.GetValue(RunValueName) as string;
        if (string.Equals(registered, BuildLaunchCommand(), StringComparison.OrdinalIgnoreCase))
        {
            existing.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }

    private string BuildLaunchCommand()
        => $"\"{_launchPath}\"";
}
