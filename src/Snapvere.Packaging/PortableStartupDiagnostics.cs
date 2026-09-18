using System.ComponentModel;
using System.Security;
using System.Text;

namespace Snapvere.Packaging;

/// <summary>
/// Keeps Portable launcher diagnostics local while returning stable,
/// non-sensitive user-facing startup guidance.
/// </summary>
public static class PortableStartupDiagnostics
{
    private const long MaximumLogBytes = 512 * 1024;
    private const int MaximumDiagnosticCharacters = 64 * 1024;
    private const string DisplayLogPath = @"%LOCALAPPDATA%\SNAPVERE\Logs\startup.log";

    public static string GetStartupLogPath()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            localApplicationData = Path.GetTempPath();
        }

        return Path.Combine(
            localApplicationData,
            "SNAPVERE",
            "Logs",
            "startup.log");
    }

    public static string GetUserFacingFailureMessage(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var guidance = exception switch
        {
            InvalidDataException =>
                "The Portable package or its local cache could not be validated. Download a fresh SNAPVERE Portable copy and try again.",
            UnauthorizedAccessException or SecurityException =>
                "Windows blocked SNAPVERE Portable from preparing its local application files. Check local security policy and folder permissions, then try again.",
            IOException =>
                "SNAPVERE Portable could not prepare its local application files. Close running SNAPVERE processes, check available disk space and try again.",
            TimeoutException =>
                "SNAPVERE Portable did not finish startup in time. Close any existing SNAPVERE process and try again.",
            PlatformNotSupportedException =>
                "This Windows architecture is not supported by this SNAPVERE Portable package.",
            Win32Exception =>
                "Windows could not start SNAPVERE from the Portable package. Restart Windows or try a fresh copy.",
            _ =>
                "SNAPVERE Portable could not start. Close SNAPVERE and try again."
        };

        return $"{guidance}{Environment.NewLine}{Environment.NewLine}" +
               $"Technical details were written locally to:{Environment.NewLine}{DisplayLogPath}";
    }

    public static void RecordLaunchFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        try
        {
            var path = GetStartupLogPath();
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);

            var diagnostic = BuildDiagnosticRecord(exception);
            var diagnosticBytes = Encoding.UTF8.GetByteCount(diagnostic);
            if (File.Exists(path))
            {
                var currentLength = new FileInfo(path).Length;
                if (currentLength + diagnosticBytes > MaximumLogBytes)
                {
                    File.Move(path, path + ".previous", overwrite: true);
                }
            }

            File.AppendAllText(path, diagnostic, Encoding.UTF8);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (SecurityException)
        {
        }
    }

    private static string BuildDiagnosticRecord(Exception exception)
    {
        var raw = exception.ToString();
        if (raw.Length > MaximumDiagnosticCharacters)
        {
            raw = raw[..MaximumDiagnosticCharacters] + Environment.NewLine + "[diagnostic truncated]";
        }

        return $"{DateTimeOffset.Now:O} | Portable startup failure{Environment.NewLine}" +
               $"{raw}{Environment.NewLine}";
    }
}
