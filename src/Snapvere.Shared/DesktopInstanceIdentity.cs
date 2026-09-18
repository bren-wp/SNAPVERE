using System.Security.Principal;

namespace Snapvere.Shared;

/// <summary>
/// Defines the canonical per-user Windows desktop-instance identity shared by
/// the WinUI application and lightweight launchers.
/// </summary>
public static class DesktopInstanceIdentity
{
    private const string MutexPrefix = "Local\\SNAPVERE.Desktop.";
    private const string ActivationEventPrefix = "Local\\SNAPVERE.Desktop.Activate.";

    public static string GetMutexName()
        => MutexPrefix + GetCurrentUserSid();

    public static string GetActivationEventName()
        => ActivationEventPrefix + GetCurrentUserSid();

    public static bool IsDesktopInstanceRunning()
    {
        if (!Mutex.TryOpenExisting(GetMutexName(), out var existing))
        {
            return false;
        }

        existing.Dispose();
        return true;
    }

    public static bool TrySignalDesktopInstance()
    {
        try
        {
            using var activationEvent = EventWaitHandle.OpenExisting(GetActivationEventName());
            return activationEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string GetCurrentUserSid()
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

        return sid;
    }
}
