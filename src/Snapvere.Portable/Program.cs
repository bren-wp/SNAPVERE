using Snapvere.Packaging;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Snapvere.Portable;

internal static class Program
{
    private const string PayloadResourceName = "Snapvere.Payload.zip";
    private const string AppExecutableName = "Snapvere.exe";

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.1";
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

                Process.Start(new ProcessStartInfo(executable)
                {
                    WorkingDirectory = cacheRoot,
                    UseShellExecute = true
                });
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
                $"SNAPVERE Portable could not start.\r\n\r\n{exception.Message}",
                "SNAPVERE Portable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.ExitCode = 1;
        }
    }

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
