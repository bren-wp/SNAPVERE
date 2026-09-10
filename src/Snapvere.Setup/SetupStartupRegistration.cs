using Microsoft.Win32;

namespace Snapvere.Setup;

internal static class SetupStartupRegistration
{
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "SNAPVERE";
    private const string AppExecutableName = "Snapvere.exe";
    private const string BackgroundArgument = "--background";

    internal static bool TrySetEnabled(string installDirectory, bool enabled, out string? warning)
    {
        try
        {
            if (enabled)
            {
                Enable(installDirectory);
            }
            else
            {
                RemoveOwnedRegistration(installDirectory);
            }

            warning = null;
            return true;
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException or
            System.Security.SecurityException or
            IOException)
        {
            warning = "Windows did not allow SNAPVERE to update the current-user startup preference. You can retry from SNAPVERE Options.";
            return false;
        }
    }

    internal static string BuildLaunchCommand(string installDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installDirectory);
        var appPath = Path.GetFullPath(Path.Combine(installDirectory, AppExecutableName));
        return $"\"{appPath}\" {BackgroundArgument}";
    }

    private static void Enable(string installDirectory)
    {
        var appPath = Path.GetFullPath(Path.Combine(installDirectory, AppExecutableName));
        if (!File.Exists(appPath))
        {
            throw new FileNotFoundException("The installed SNAPVERE executable is missing.", appPath);
        }

        using var key = Registry.CurrentUser.CreateSubKey(RunRegistryPath, writable: true)
            ?? throw new InvalidOperationException("Windows could not open the current-user startup registry key.");
        key.SetValue(RunValueName, BuildLaunchCommand(installDirectory), RegistryValueKind.String);
    }

    private static void RemoveOwnedRegistration(string installDirectory)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true);
        if (key is null)
        {
            return;
        }

        var current = key.GetValue(RunValueName) as string;
        if (string.Equals(current, BuildLaunchCommand(installDirectory), StringComparison.OrdinalIgnoreCase))
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }
}
