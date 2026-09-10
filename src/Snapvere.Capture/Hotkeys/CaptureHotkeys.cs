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
    private const uint Key4 = 0x34;

    private const HotkeyModifiers StandardModifiers =
        HotkeyModifiers.Control |
        HotkeyModifiers.Shift |
        HotkeyModifiers.NoRepeat;

    private static readonly CaptureHotkeyBinding PrintScreenRegion =
        new(0x5300, CaptureMode.Region, HotkeyModifiers.NoRepeat, PrintScreenKey, "Print Screen");

    private static readonly IReadOnlyList<CaptureHotkeyBinding> AllBindings =
    [
        PrintScreenRegion,
        new(0x5301, CaptureMode.Region, StandardModifiers, Key1, "Ctrl+Shift+1"),
        new(0x5302, CaptureMode.Window, StandardModifiers, Key2, "Ctrl+Shift+2"),
        new(0x5303, CaptureMode.Scrolling, StandardModifiers, Key3, "Ctrl+Shift+3"),
        new(0x5304, CaptureMode.FullScreen, StandardModifiers, Key4, "Ctrl+Shift+4")
    ];

    private static readonly IReadOnlyList<CaptureHotkeyBinding> Implemented =
    [
        AllBindings[0],
        AllBindings[1],
        AllBindings[2],
        AllBindings[4]
    ];

    public static IReadOnlyList<CaptureHotkeyBinding> All => AllBindings;

    /// <summary>
    /// Region capture deliberately has two bindings. Print Screen provides the
    /// expected screenshot-app workflow when Windows permits it, while
    /// Ctrl+Shift+1 remains a conflict-safe fallback. Window Capture is exposed
    /// through Ctrl+Shift+2 now that its picker and WGC workflow are implemented.
    /// Scrolling stays unregistered until its workflow is genuinely implemented.
    /// </summary>
    public static IReadOnlyList<CaptureHotkeyBinding> ImplementedNow => Implemented;
}
