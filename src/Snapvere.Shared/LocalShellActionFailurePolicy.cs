using System.ComponentModel;
using System.Diagnostics;
using System.Security;

namespace Snapvere.Shared;

/// <summary>
/// Defines the bounded exception taxonomy expected when a user-triggered action
/// asks Windows to open a local file or folder through the shell.
/// </summary>
public static class LocalShellActionFailurePolicy
{
    public static bool IsExpected(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is IOException or
            UnauthorizedAccessException or
            SecurityException or
            InvalidOperationException or
            Win32Exception;
    }
}


/// <summary>
/// Opens a local file, folder or shell URI through the Windows shell and treats
/// a missing shell handler as an explicit failure instead of a silent success.
/// </summary>
public static class LocalShellAction
{
    public static void Open(
        string target,
        Func<ProcessStartInfo, Process?>? starter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        starter ??= static startInfo => Process.Start(startInfo);
        using var process = starter(new ProcessStartInfo(target)
        {
            UseShellExecute = true
        });

        if (process is null)
        {
            throw new InvalidOperationException(
                "Windows did not start a shell handler for the requested target.");
        }
    }
}
