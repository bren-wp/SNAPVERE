using Microsoft.Win32;
using Snapvere.Packaging;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Snapvere.Setup;

internal sealed record InstallerResult(bool Succeeded, int ExitCode, string Message);

internal static class InstallerEngine
{
    private const string ProductName = "SNAPVERE";
    private const string AppExecutableName = "Snapvere.exe";
    private const string InstalledSetupName = "SNAPVERE-Setup.exe";
    private const string InstallationMarkerName = ".snapvere-installation";
    private const string InstallationMarkerPrefix = "SNAPVERE-INSTALLATION-V1";
    private const string LicenseResourceName = "Snapvere.License.txt";
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\SNAPVERE";
    private const string StartupRunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupRunValueName = "SNAPVERE";
    private const uint MoveFileDelayUntilReboot = 0x00000004;

    public static string VersionText =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.1";

    public static string GetDefaultInstallDirectory()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            ProductName);

    public static string ReadLicenseText()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LicenseResourceName)
            ?? throw new InvalidOperationException("The SNAPVERE license resource is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static InstallerResult Install(
        string installDirectory,
        bool createStartMenuShortcut,
        bool createDesktopShortcut,
        bool silent,
        Action<int>? progress)
    {
        try
        {
            var installRoot = ValidateInstallDirectory(installDirectory);
            var parent = Directory.GetParent(installRoot)?.FullName
                ?? throw new InvalidOperationException("The selected installation directory has no parent folder.");
            Directory.CreateDirectory(parent);

            if (!TryCloseRunningApplication(installRoot, out var runningMessage))
            {
                return new InstallerResult(false, 1618, runningMessage);
            }

            var stagingRoot = Path.Combine(parent, $".snapvere-stage-{Guid.NewGuid():N}");
            var backupRoot = Path.Combine(parent, $".snapvere-backup-{Guid.NewGuid():N}");
            var previousInstallMoved = false;

            try
            {
                using var payload = UniversalPayload.OpenEmbeddedPayload(
                    Assembly.GetExecutingAssembly(),
                    out var payloadArchitecture);

                EmbeddedPayload.ExtractZipSafely(
                    payload,
                    stagingRoot,
                    (completed, total) => progress?.Invoke(total == 0 ? 0 : Math.Clamp(completed * 75 / total, 0, 75)));

                var stagedExecutable = Path.Combine(stagingRoot, AppExecutableName);
                if (!File.Exists(stagedExecutable))
                {
                    throw new InvalidDataException("The SNAPVERE package does not contain Snapvere.exe.");
                }

                File.WriteAllText(
                    Path.Combine(stagingRoot, InstallationMarkerName),
                    string.Join(
                        Environment.NewLine,
                        InstallationMarkerPrefix,
                        VersionText,
                        UniversalPayload.GetToken(payloadArchitecture),
                        string.Empty));

                progress?.Invoke(78);

                if (Directory.Exists(installRoot))
                {
                    Directory.Move(installRoot, backupRoot);
                    previousInstallMoved = true;
                }

                Directory.Move(stagingRoot, installRoot);
                progress?.Invoke(84);

                var currentSetupPath = Environment.ProcessPath
                    ?? throw new InvalidOperationException("Windows did not provide the setup executable path.");
                File.Copy(currentSetupPath, Path.Combine(installRoot, InstalledSetupName), overwrite: true);

                UpdateShortcut(
                    createStartMenuShortcut,
                    GetStartMenuShortcutPath(),
                    installRoot);
                UpdateShortcut(
                    createDesktopShortcut,
                    GetDesktopShortcutPath(),
                    installRoot);

                progress?.Invoke(92);
                WriteUninstallRegistration(installRoot);
                progress?.Invoke(100);

                if (previousInstallMoved)
                {
                    EmbeddedPayload.DeleteDirectoryBestEffort(backupRoot);
                }

                return new InstallerResult(
                    true,
                    0,
                    $"SNAPVERE {VersionText} ({UniversalPayload.GetToken(payloadArchitecture)}) was installed successfully.");
            }
            catch
            {
                EmbeddedPayload.DeleteDirectoryBestEffort(stagingRoot);
                if (previousInstallMoved && Directory.Exists(backupRoot))
                {
                    EmbeddedPayload.DeleteDirectoryBestEffort(installRoot);
                    Directory.Move(backupRoot, installRoot);
                }

                throw;
            }
        }
        catch (UnauthorizedAccessException exception)
        {
            return new InstallerResult(false, 5, silent ? exception.Message : "Windows denied access to the selected installation folder.");
        }
        catch (IOException exception)
        {
            return new InstallerResult(false, 1, silent ? exception.Message : $"SNAPVERE setup could not write the application files. {exception.Message}");
        }
        catch (Exception exception)
        {
            return new InstallerResult(false, 1, exception.Message);
        }
    }

    public static InstallerResult Uninstall(bool silent)
    {
        try
        {
            var installRoot = ReadRegisteredInstallDirectory() ?? GetDefaultInstallDirectory();
            installRoot = ValidateExistingInstallForRemoval(installRoot);

            if (!TryCloseRunningApplication(installRoot, out var runningMessage))
            {
                return new InstallerResult(false, 1618, runningMessage);
            }

            DeleteInstalledStartupRegistration(installRoot);

            var currentSetupPath = Environment.ProcessPath;
            if (currentSetupPath is not null && IsPathInside(currentSetupPath, installRoot))
            {
                StartDeferredCleanup(currentSetupPath, installRoot);
                RemoveRegistrationsAndShortcuts();
            }
            else
            {
                RemoveRegistrationsAndShortcuts();
                DeleteValidatedInstallationWithRetries(installRoot);
            }

            return new InstallerResult(true, 0, $"SNAPVERE {VersionText} was removed from this Windows account.");
        }
        catch (UnauthorizedAccessException exception)
        {
            return new InstallerResult(false, 5, silent ? exception.Message : "Windows denied access while removing SNAPVERE.");
        }
        catch (Exception exception)
        {
            return new InstallerResult(false, 1, exception.Message);
        }
    }

    public static InstallerResult CompleteDeferredUninstall(
        string installDirectory,
        int waitForProcessId,
        bool silent)
    {
        try
        {
            WaitForProcessExit(waitForProcessId);
            var installRoot = ValidateExistingInstallForRemoval(installDirectory);
            DeleteValidatedInstallationWithRetries(installRoot);
            ScheduleMaintenanceSelfCleanup();
            return new InstallerResult(true, 0, "SNAPVERE cleanup completed.");
        }
        catch (Exception exception)
        {
            return new InstallerResult(false, 1, silent ? exception.Message : "SNAPVERE could not complete uninstall cleanup.");
        }
    }

    public static bool LaunchInstalledApplication(string installDirectory)
    {
        try
        {
            var executable = Path.Combine(Path.GetFullPath(installDirectory), AppExecutableName);
            if (!File.Exists(executable))
            {
                return false;
            }

            _ = Process.Start(new ProcessStartInfo(executable)
            {
                WorkingDirectory = Path.GetDirectoryName(executable)!,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void UpdateShortcut(bool enabled, string shortcutPath, string installRoot)
    {
        if (!enabled)
        {
            DeleteFileBestEffort(shortcutPath);
            return;
        }

        ShortcutService.CreateShortcut(
            shortcutPath,
            Path.Combine(installRoot, AppExecutableName),
            installRoot,
            "SNAPVERE — Capture anything.");
    }

    private static void RemoveRegistrationsAndShortcuts()
    {
        DeleteFileBestEffort(GetStartMenuShortcutPath());
        DeleteFileBestEffort(GetDesktopShortcutPath());
        DeleteUninstallRegistration();
    }

    private static void WaitForProcessExit(int processId)
    {
        if (processId <= 0)
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            _ = process.WaitForExit(20_000);
        }
        catch (ArgumentException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static string ValidateInstallDirectory(string installDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(installDirectory);
        var fullPath = Path.GetFullPath(installDirectory.Trim());
        var root = Path.GetPathRoot(fullPath);
        if (string.Equals(
                fullPath.TrimEnd(Path.DirectorySeparatorChar),
                root?.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("SNAPVERE cannot be installed directly into a drive root.");
        }

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrWhiteSpace(windows) && IsPathInside(fullPath, windows))
        {
            throw new InvalidOperationException("SNAPVERE cannot be installed inside the Windows system directory.");
        }

        return fullPath;
    }

    private static string ValidateExistingInstallForRemoval(string installDirectory)
    {
        var fullPath = ValidateInstallDirectory(installDirectory);
        if (!Directory.Exists(fullPath))
        {
            return fullPath;
        }

        var markerPath = Path.Combine(fullPath, InstallationMarkerName);
        if (!File.Exists(markerPath))
        {
            throw new InvalidOperationException("The registered path has no SNAPVERE installation marker. Nothing was removed.");
        }

        var marker = File.ReadAllText(markerPath);
        if (!marker.StartsWith(InstallationMarkerPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The registered path has an invalid SNAPVERE installation marker. Nothing was removed.");
        }

        var hasApp = File.Exists(Path.Combine(fullPath, AppExecutableName));
        var hasSetup = File.Exists(Path.Combine(fullPath, InstalledSetupName));
        if (!hasApp && !hasSetup)
        {
            throw new InvalidOperationException("The registered SNAPVERE path does not contain SNAPVERE application files. Nothing was removed.");
        }

        return fullPath;
    }

    private static bool TryCloseRunningApplication(string installRoot, out string message)
    {
        foreach (var process in Process.GetProcessesByName("Snapvere"))
        {
            using (process)
            {
                try
                {
                    var processPath = process.MainModule?.FileName;
                    if (string.IsNullOrWhiteSpace(processPath) || !IsPathInside(processPath, installRoot))
                    {
                        continue;
                    }

                    _ = process.CloseMainWindow();
                    if (!process.WaitForExit(3000))
                    {
                        message = "Close SNAPVERE before installing or removing this version, then try again.";
                        return false;
                    }
                }
                catch (InvalidOperationException)
                {
                }
                catch (System.ComponentModel.Win32Exception)
                {
                }
            }
        }

        message = string.Empty;
        return true;
    }

    private static bool IsPathInside(string candidate, string parent)
    {
        var candidateFullPath = Path.GetFullPath(candidate);
        var parentFullPath = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return candidateFullPath.StartsWith(parentFullPath, StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteUninstallRegistration(string installRoot)
    {
        using var key = Registry.CurrentUser.CreateSubKey(UninstallRegistryPath, writable: true)
            ?? throw new InvalidOperationException("Windows could not create the SNAPVERE uninstall registration.");

        var setupPath = Path.Combine(installRoot, InstalledSetupName);
        var appPath = Path.Combine(installRoot, AppExecutableName);
        key.SetValue("DisplayName", ProductName, RegistryValueKind.String);
        key.SetValue("DisplayVersion", VersionText, RegistryValueKind.String);
        key.SetValue("Publisher", "Brendigo", RegistryValueKind.String);
        key.SetValue("InstallLocation", installRoot, RegistryValueKind.String);
        key.SetValue("DisplayIcon", appPath, RegistryValueKind.String);
        key.SetValue("UninstallString", $"\"{setupPath}\" --uninstall", RegistryValueKind.String);
        key.SetValue("QuietUninstallString", $"\"{setupPath}\" --uninstall --silent", RegistryValueKind.String);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("EstimatedSize", CalculateEstimatedSizeKilobytes(installRoot), RegistryValueKind.DWord);
    }

    private static string? ReadRegisteredInstallDirectory()
    {
        using var key = Registry.CurrentUser.OpenSubKey(UninstallRegistryPath, writable: false);
        return key?.GetValue("InstallLocation") as string;
    }

    private static void DeleteUninstallRegistration()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(UninstallRegistryPath, throwOnMissingSubKey: false);
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void DeleteInstalledStartupRegistration(string installRoot)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRunRegistryPath, writable: true);
            var registeredCommand = key?.GetValue(StartupRunValueName) as string;
            if (string.IsNullOrWhiteSpace(registeredCommand))
            {
                return;
            }

            var installedAppPath = Path.GetFullPath(Path.Combine(installRoot, AppExecutableName));
            var expectedCommand = $"\"{installedAppPath}\"";
            if (string.Equals(registeredCommand, expectedCommand, StringComparison.OrdinalIgnoreCase))
            {
                key!.DeleteValue(StartupRunValueName, throwOnMissingValue: false);
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (System.Security.SecurityException)
        {
        }
    }

    private static int CalculateEstimatedSizeKilobytes(string installRoot)
    {
        long bytes = 0;
        foreach (var file in Directory.EnumerateFiles(installRoot, "*", SearchOption.AllDirectories))
        {
            try
            {
                bytes = checked(bytes + new FileInfo(file).Length);
            }
            catch (IOException)
            {
            }
        }

        return checked((int)Math.Min(int.MaxValue, (bytes + 1023) / 1024));
    }

    private static string GetStartMenuShortcutPath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            ProductName);
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "SNAPVERE.lnk");
    }

    private static string GetDesktopShortcutPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "SNAPVERE.lnk");

    private static void StartDeferredCleanup(string currentSetupPath, string installRoot)
    {
        var maintenanceRoot = Path.Combine(
            Path.GetTempPath(),
            "SNAPVERE",
            "Maintenance",
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(maintenanceRoot);

        var maintenanceSetup = Path.Combine(maintenanceRoot, InstalledSetupName);
        File.Copy(currentSetupPath, maintenanceSetup, overwrite: false);

        var startInfo = new ProcessStartInfo(maintenanceSetup)
        {
            WorkingDirectory = maintenanceRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        startInfo.ArgumentList.Add("--cleanup-install");
        startInfo.ArgumentList.Add(installRoot);
        startInfo.ArgumentList.Add("--wait-pid");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("--silent");

        _ = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows could not start the SNAPVERE maintenance cleanup process.");
    }

    private static void DeleteValidatedInstallationWithRetries(string installRoot)
    {
        if (!Directory.Exists(installRoot))
        {
            return;
        }

        _ = ValidateExistingInstallForRemoval(installRoot);

        Exception? lastError = null;
        for (var attempt = 0; attempt < 12; attempt++)
        {
            try
            {
                Directory.Delete(installRoot, recursive: true);
                return;
            }
            catch (IOException exception)
            {
                lastError = exception;
                Thread.Sleep(250);
            }
            catch (UnauthorizedAccessException exception)
            {
                lastError = exception;
                Thread.Sleep(250);
            }
        }

        throw new IOException("SNAPVERE application files remained locked after uninstall retries.", lastError);
    }

    private static void ScheduleMaintenanceSelfCleanup()
    {
        var currentPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return;
        }

        _ = NativeMethods.MoveFileEx(currentPath, null, MoveFileDelayUntilReboot);
        var directory = Path.GetDirectoryName(currentPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            _ = NativeMethods.MoveFileEx(directory, null, MoveFileDelayUntilReboot);
        }
    }

    private static void DeleteFileBestEffort(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static class ShortcutService
    {
        internal static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string description)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
            var shellType = Type.GetTypeFromProgID("WScript.Shell")
                ?? throw new InvalidOperationException("Windows Script Host is unavailable, so the shortcut could not be created.");
            var shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Windows could not create the shortcut service.");
            object? shortcut = null;

            try
            {
                shortcut = shellType.InvokeMember(
                    "CreateShortcut",
                    BindingFlags.InvokeMethod,
                    binder: null,
                    target: shell,
                    args: [shortcutPath]);

                if (shortcut is null)
                {
                    throw new InvalidOperationException("Windows could not create the SNAPVERE shortcut.");
                }

                var shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, [targetPath]);
                shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, [workingDirectory]);
                shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, [description]);
                shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, [$"{targetPath},0"]);
                _ = shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            finally
            {
                if (shortcut is not null && Marshal.IsComObject(shortcut))
                {
                    _ = Marshal.FinalReleaseComObject(shortcut);
                }

                if (Marshal.IsComObject(shell))
                {
                    _ = Marshal.FinalReleaseComObject(shell);
                }
            }
        }
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", EntryPoint = "MoveFileExW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool MoveFileEx(string existingFileName, string? newFileName, uint flags);
    }
}
