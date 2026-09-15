using System.Security.Principal;

namespace Snapvere.Shared;

/// <summary>
/// Defines the canonical per-user Windows desktop-instance identity shared by
/// the WinUI application and lightweight launchers that need to avoid doing
/// expensive preparation when SNAPVERE is already running.
/// </summary>
public static class DesktopInstanceIdentity
{
    private const string MutexPrefix = "Local\\SNAPVERE.Desktop.";

    public static string GetMutexName()
    {
        string? sid;
        using (var identity = WindowsIdentity.GetCurrent())
        {
            sid = identity.User?.Value;
        }

        if (string.IsNullOrWhiteSpace(sid))
        {
            throw new InvalidOperationException(
                "SNAPVERE could not resolve the current Windows user identity.");
        }

        return MutexPrefix + sid;
    }

    public static bool IsDesktopInstanceRunning()
    {
        if (!Mutex.TryOpenExisting(GetMutexName(), out var existing))
        {
            return false;
        }

        existing.Dispose();
        return true;
    }
}
