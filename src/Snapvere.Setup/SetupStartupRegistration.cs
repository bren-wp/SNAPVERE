using Microsoft.Win32;

namespace Snapvere.Setup;

internal static class SetupStartupRegistration
{
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "SNAPVERE";
    private const string AppExecutableName = "Snapvere.exe";

    internal static void SetEnabled(string installDirectory, bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installDirectory);
        var appPath = Path.GetFullPath(Path.Combine(installDirectory, AppExecutableName));

        using var key = Registry.CurrentUser.CreateSubKey(RunRegistryPath, writable: true)
            ?? throw new InvalidOperationException("Windows could not open the current-user startup registry key.");

        if (enabled)
        {
            if (!File.Exists(appPath))
            {
                throw new FileNotFoundException("The installed SNAPVERE executable is missing.", appPath);
            }

            key.SetValue(RunValueName, $"\"{appPath}\"", RegistryValueKind.String);
            return;
        }

        var current = key.GetValue(RunValueName) as string;
        if (string.Equals(current, $"\"{appPath}\"", StringComparison.OrdinalIgnoreCase))
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }
}
