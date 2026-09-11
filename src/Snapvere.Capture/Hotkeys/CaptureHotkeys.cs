using Snapvere.Domain.Capture;

namespace Snapvere.Capture.Hotkeys;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008,
    NoRepeat = 0x4000
}

public sealed record CaptureHotkeyBinding(
    int Id,
    CaptureMode Mode,
    HotkeyModifiers Modifiers,
    uint VirtualKey,
    string GestureText);

public sealed record HotkeyRegistrationFailure(
    CaptureHotkeyBinding Binding,
    int NativeErrorCode);

public sealed record GlobalHotkeyRegistrationReport(
    IReadOnlyList<CaptureHotkeyBinding> Registered,
    IReadOnlyList<HotkeyRegistrationFailure> Conflicts)
{
    public bool HasConflicts => Conflicts.Count > 0;
}

public sealed class CaptureHotkeyPressedEventArgs(CaptureHotkeyBinding binding) : EventArgs
{
    public CaptureHotkeyBinding Binding { get; } = binding;
}

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler<CaptureHotkeyPressedEventArgs>? HotkeyPressed;

    GlobalHotkeyRegistrationReport Start();
}

public static class DefaultCaptureHotkeys
{
    private const uint PrintScreenKey = 0x2C;
    private const uint Key1 = 0x31;
    private const uint Key2 = 0x32;
    private const uint Key3 = 0x33;

    private const HotkeyModifiers StandardModifiers =
        HotkeyModifiers.Control |
        HotkeyModifiers.Shift |
        HotkeyModifiers.NoRepeat;

    private static readonly IReadOnlyList<CaptureHotkeyBinding> AllBindings =
    [
        new(0x5300, CaptureMode.Region, HotkeyModifiers.NoRepeat, PrintScreenKey, "Print Screen"),
        new(0x5301, CaptureMode.Region, StandardModifiers, Key1, "Ctrl+Shift+1"),
        new(0x5302, CaptureMode.Window, StandardModifiers, Key2, "Ctrl+Shift+2"),
        new(0x5303, CaptureMode.FullScreen, StandardModifiers, Key3, "Ctrl+Shift+3")
    ];

    /// <summary>
    /// Every published binding is implemented. Region capture deliberately has
    /// two bindings: Print Screen provides the expected screenshot workflow
    /// when Windows permits it, while Ctrl+Shift+1 remains a conflict-safe
    /// fallback. Window Capture uses Ctrl+Shift+2 and Full Screen uses
    /// Ctrl+Shift+3. Scrolling capture is not assigned a global hotkey until a
    /// real scrolling workflow exists.
    /// </summary>
    public static IReadOnlyList<CaptureHotkeyBinding> All => AllBindings;

    public static IReadOnlyList<CaptureHotkeyBinding> ImplementedNow => AllBindings;
}
