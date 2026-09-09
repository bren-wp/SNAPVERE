namespace Snapvere.Setup;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var uninstall = args.Any(argument =>
            string.Equals(argument, "--uninstall", StringComparison.OrdinalIgnoreCase));
        var silent = args.Any(argument =>
            string.Equals(argument, "--silent", StringComparison.OrdinalIgnoreCase));
        var acceptLicense = args.Any(argument =>
            string.Equals(argument, "--accept-license", StringComparison.OrdinalIgnoreCase));

        if (silent)
        {
            if (!uninstall && !acceptLicense)
            {
                Environment.ExitCode = 2;
                return;
            }

            var result = uninstall
                ? InstallerEngine.Uninstall(silent: true)
                : InstallerEngine.Install(
                    InstallerEngine.GetDefaultInstallDirectory(),
                    createStartMenuShortcut: true,
                    createDesktopShortcut: false,
                    silent: true,
                    progress: null);
            Environment.ExitCode = result.ExitCode;
            return;
        }

        Application.Run(new SetupForm(uninstall));
    }
}
