using System.Runtime.InteropServices;
using System.Text;

namespace Snapvere.App.Services;

internal static class StartupDiagnostics
{
    private const long MaximumLogBytes = 512 * 1024;
    private const int MaximumExceptionDepth = 8;
    private static int _initialized;

    public static string LogFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SNAPVERE",
        "Logs",
        "startup.log");

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0)
        {
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                Record("AppDomain.UnhandledException", exception);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Record("TaskScheduler.UnobservedTaskException", args.Exception);
        };

        WriteLine(
            $"Startup begin | SNAPVERE {typeof(StartupDiagnostics).Assembly.GetName().Version?.ToString(3) ?? "unknown"} | " +
            $"OS={RuntimeInformation.OSDescription} | Process={RuntimeInformation.ProcessArchitecture} | Framework={RuntimeInformation.FrameworkDescription}");
    }

    public static void Record(string stage, Exception exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        ArgumentNullException.ThrowIfNull(exception);

        var builder = new StringBuilder();
        builder.Append(DateTimeOffset.Now.ToString("O"));
        builder.Append(" | ");
        builder.AppendLine(stage);
        AppendException(builder, exception, depth: 0);

        WriteRaw(builder.ToString());
    }

    public static void WriteLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        WriteRaw($"{DateTimeOffset.Now:O} | {message}{Environment.NewLine}");
    }

    public static void ShowFatal(string stage, Exception exception)
    {
        Record(stage, exception);

        var message =
            "SNAPVERE could not start correctly.\r\n\r\n" +
            $"{exception.Message}\r\n\r\n" +
            "A local diagnostic log was written to:\r\n" +
            LogFilePath;

        _ = NativeMethods.MessageBoxW(
            nint.Zero,
            message,
            "SNAPVERE startup error",
            NativeMethods.MbOk | NativeMethods.MbIconError | NativeMethods.MbSetForeground);
    }

    private static void AppendException(StringBuilder builder, Exception exception, int depth)
    {
        if (depth >= MaximumExceptionDepth)
        {
            builder.AppendLine("Exception chain truncated.");
            return;
        }

        builder.Append("Exception[");
        builder.Append(depth);
        builder.Append("] Type=");
        builder.Append(exception.GetType().FullName);
        builder.Append(" | HResult=0x");
        builder.Append(exception.HResult.ToString("X8", System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(" | Message=");
        builder.AppendLine(exception.Message);

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            builder.AppendLine(exception.StackTrace);
        }

        if (exception.InnerException is not null)
        {
            builder.AppendLine("Inner exception:");
            AppendException(builder, exception.InnerException, depth + 1);
        }
    }

    private static void WriteRaw(string text)
    {
        try
        {
            var path = LogFilePath;
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);

            if (File.Exists(path) && new FileInfo(path).Length > MaximumLogBytes)
            {
                var previous = path + ".previous";
                try
                {
                    File.Move(path, previous, overwrite: true);
                }
                catch (IOException)
                {
                    File.Delete(path);
                }
            }

            File.AppendAllText(path, text, Encoding.UTF8);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static class NativeMethods
    {
        internal const uint MbOk = 0x00000000;
        internal const uint MbIconError = 0x00000010;
        internal const uint MbSetForeground = 0x00010000;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int MessageBoxW(nint window, string text, string caption, uint type);
    }
}
