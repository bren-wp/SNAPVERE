using Snapvere.Packaging;
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
            Environment.ExitCode = EmbeddedAppLauncher.Launch(
                Assembly.GetExecutingAssembly(),
                args);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"SNAPVERE Portable could not start.\r\n\r\n{exception.Message}\r\n\r\nStartup log (when available):\r\n{EmbeddedAppLauncher.GetStartupLogPath()}",
                "SNAPVERE Portable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Environment.ExitCode = 1;
        }
    }
}
