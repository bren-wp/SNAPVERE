using Snapvere.Packaging;
using Snapvere.Shared;
using System.Reflection;

namespace Snapvere.Portable;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            // Avoid hashing/extracting the large universal payload when the
            // per-user SNAPVERE desktop instance is already alive. The child
            // process still owns the authoritative mutex; this is only a fast
            // launcher path and does not replace the in-app single-instance guard.
            if (DesktopInstanceIdentity.IsDesktopInstanceRunning())
            {
                Environment.ExitCode = 0;
                return;
            }

            ApplicationConfiguration.Initialize();
            Environment.ExitCode = EmbeddedAppLauncher.Launch(
                Assembly.GetExecutingAssembly(),
                args);
        }
        catch (Exception exception)
        {
            ApplicationConfiguration.Initialize();
            MessageBox.Show(
                $"SNAPVERE Portable could not start.\r\n\r\n{exception.Message}\r\n\r\nStartup log (when available):\r\n{EmbeddedAppLauncher.GetStartupLogPath()}",
                "SNAPVERE Portable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.ExitCode = 1;
        }
    }
}
