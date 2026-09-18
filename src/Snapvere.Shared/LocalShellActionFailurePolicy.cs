using System.ComponentModel;
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
