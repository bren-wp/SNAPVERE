using Snapvere.Packaging;
using Snapvere.Shared;
using System.Reflection;

namespace Snapvere.Portable;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

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

            Environment.ExitCode = EmbeddedAppLauncher.Launch(
                Assembly.GetExecutingAssembly(),
                args);
        }
        catch (Exception exception)
        {
            PortableStartupDiagnostics.RecordLaunchFailure(exception);
            MessageBox.Show(
                PortableStartupDiagnostics.GetUserFacingFailureMessage(exception),
                "SNAPVERE Portable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.ExitCode = 1;
        }
    }
}
